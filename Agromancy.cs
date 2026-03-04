using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using GenericModConfigMenu;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Agromancy.Config;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using Agromancy.Helpers;
using Agromancy.Commands;
using Agromancy.Menus;
using Agromancy.Models;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using StardewValley.GameData.Buffs;
using StardewValley.GameData.Crops;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Tools;
using StardewValley.Mods;
using StardewValley.TerrainFeatures;

namespace Agromancy
{
    internal sealed class Agromancy : Mod
    {
        internal static IModHelper ModHelper { get; set; } = null!;
        internal static IMonitor ModMonitor { get; set; } = null!;
        internal static IManifest Manifest { get; set; } = null!;
        private static CommandHandler CommandHandler { get; set; } = null!;
        internal static ModConfig Config { get; set; } = null!;
        internal static Harmony Harmony { get; set; } = null!;

        internal static Texture2D? ScreenTexture { get; set; } = null!;
        
        internal static Texture2D PerlinNoise { get; set; } = null!;

        /* Shaders */
        public static Effect GrayscaleFx = null!;
        public static Effect BlurFx = null!;
        public static Effect StatsFx = null!;
        public static Effect LiquidCircleFx = null!;

        internal static string UNIQUE_ID => Manifest.UniqueID;

        internal static CropManager CropManager { get; set; } = null!;

        public override void Entry(IModHelper helper)
        {
            i18n.Init(helper.Translation);
            ModHelper = helper;
            ModMonitor = Monitor;
            Manifest = ModManifest;
            CommandHandler = new CommandHandler(ModHelper, ModManifest, "agromancy");
            CommandHandler.Register();
            Config = helper.ReadConfig<ModConfig>();
            Harmony = new Harmony(ModManifest.UniqueID);
            
            PerlinNoise = Helper.ModContent.Load<Texture2D>("assets/noiseTexture.png");

            try
            {
                byte[] stream = File.ReadAllBytes(Path.Combine(helper.DirectoryPath, "assets/shaders/grayscale.mgfx"));
                GrayscaleFx = new Effect(Game1.graphics.GraphicsDevice, stream);
                byte[] blurStream = File.ReadAllBytes(Path.Combine(helper.DirectoryPath, "assets/shaders/blur.mgfx"));
                BlurFx = new Effect(Game1.graphics.GraphicsDevice, blurStream);
                byte[] statsStream = File.ReadAllBytes(Path.Combine(helper.DirectoryPath, "assets/shaders/stats.mgfx"));
                StatsFx = new Effect(Game1.graphics.GraphicsDevice, statsStream);
                byte[] liquidCircleStream = File.ReadAllBytes(Path.Combine(helper.DirectoryPath, "assets/shaders/liquidcircle.mgfx"));
                LiquidCircleFx = new Effect(Game1.graphics.GraphicsDevice, liquidCircleStream);
            }
            catch (Exception e)
            {
                Log.Error("Error loading shaders: " + e);
            }

            Harmony.PatchAll();

            Helper.Events.Input.ButtonPressed += OnButtonPressed;
            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            Helper.Events.Display.RenderedWorld += OnRenderedWorld;
            Helper.Events.Display.MenuChanged += OnMenuChanged;
            Helper.Events.Content.AssetRequested += OnAssetsRequested;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu != null) Config.SetupConfig(configMenu, ModManifest, Helper);
            CropManager = new CropManager();
        }

        private void OnAssetsRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Crops"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, CropData>().Data;
                    data["472"].HarvestMinQuality = 2;
                });
            }

            if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, ObjectData>().Data;
                    data[$"{UNIQUE_ID}_EssenceVial"] = new ObjectData()
                    {
                        Name = $"{UNIQUE_ID}_EssenceVial",
                        DisplayName = "Essence Vial", // TODO: i18n
                        Description = "A capsule capable of storing a seemingly limitless amount of magical essence.\n\nContains {6} essence.\n- {0} Yield\n- {1} Quality\n- {2} Growth\n- {3} Giant\n- {4} Water\n- {5} Seed", // TODO: i18n
                        Type = "Basic",
                        Category = 0,
                        Price = 100,
                        Texture = $"{UNIQUE_ID}/Objects",
                        SpriteIndex = 0,
                    };
                });
            }

            if (e.NameWithoutLocale.IsEquivalentTo("Data/Tools"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, ToolData>().Data;
                    data[$"{UNIQUE_ID}_Agrometer"] = new ToolData()
                    {
                        ClassName = "GenericTool",
                        Name = $"{UNIQUE_ID}_Agrometer",
                        DisplayName = "Agrometer", // TODO: i18n
                        Description = "A magical tool that allows you to visualize the magical essences of your crops.", // TODO: i18n
                        Texture = $"{UNIQUE_ID}/Objects",
                        SpriteIndex = 1,
                        AttachmentSlots = 1,
                        CanBeLostOnDeath = false,
                        SetProperties = new Dictionary<string, string>()
                        {
                            { "InstantUse", "true" },
                            { "IsEfficient", "true" }
                        }
                    };
                });
            }

            if (e.NameWithoutLocale.IsEquivalentTo($"{UNIQUE_ID}/AgrometerFrame"))
            {
                e.LoadFromModFile<Texture2D>("assets/menuBG_Leafy.png", AssetLoadPriority.Exclusive);
            }

            if (e.NameWithoutLocale.IsEquivalentTo($"{UNIQUE_ID}/AgrometerCircles"))
            {
                e.LoadFromModFile<Texture2D>("assets/menuBG_circles_empty.png", AssetLoadPriority.Exclusive);
            }

            if (e.NameWithoutLocale.IsEquivalentTo($"{UNIQUE_ID}/AgrometerStatRing"))
            {
                e.LoadFromModFile<Texture2D>("assets/menu_StatRing.png", AssetLoadPriority.Exclusive);
            }

            if (e.NameWithoutLocale.IsEquivalentTo($"{UNIQUE_ID}/MonochromeArrows"))
            {
                e.LoadFromModFile<Texture2D>("assets/arrows.png", AssetLoadPriority.Exclusive);
            }

            if (e.NameWithoutLocale.IsEquivalentTo($"{UNIQUE_ID}/Objects"))
            {
                e.LoadFromModFile<Texture2D>("assets/objects.png", AssetLoadPriority.Exclusive);
            }
        }

        private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
        {
            if (e.NewMenu is AgrometerMenu)
            {
                Game1.player.viewingLocation.Value = Game1.currentLocation.Name;
            } else if (e.OldMenu is AgrometerMenu)
            {
                Game1.player.viewingLocation.Value = null;
            }
        }

        private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
        {
            if (Game1.activeClickableMenu is not AgrometerMenu menu)
            {
                return;
            }
            
            e.SpriteBatch.Draw(
                texture: Game1.staminaRect,
                destinationRectangle: e.SpriteBatch.GraphicsDevice.Viewport.Bounds,
                sourceRectangle: null,
                color: Color.Black * 0.15f,
                rotation: 0f,
                origin: Vector2.Zero,
                effects: SpriteEffects.None,
                layerDepth: 0.9f
            );
            
            if (e.SpriteBatch.GraphicsDevice.GetRenderTargets().FirstOrDefault().RenderTarget is not RenderTarget2D
                target) return;
            
            BlurFx.Parameters["Saturation"].SetValue(0.35f);
            BlurFx.Parameters["Resolution"].SetValue(new Vector2(target.Width, target.Height));
            BlurFx.Parameters["BlurMultiplier"].SetValue(3f);
            BlurFx.Parameters["ClarityCenter"].SetValue(new Vector2(menu.GetAgrometerCenter().X / Game1.uiViewport.Width, menu.GetAgrometerCenter().Y / Game1.uiViewport.Height));
            BlurFx.Parameters["ClarityRadius"].SetValue(Game1.viewport.Height * 0.33f);
            BlurFx.Parameters["InvertClarity"].SetValue(ModHelper.Input.IsDown(SButton.RightShift));

            e.SpriteBatch.End();
            e.SpriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                effect: BlurFx
            );
            
            e.SpriteBatch.Draw(
                texture: target,
                destinationRectangle: e.SpriteBatch.GraphicsDevice.Viewport.Bounds,
                sourceRectangle: null,
                color: Color.White,
                rotation: 0f,
                origin: Vector2.Zero,
                effects: SpriteEffects.None,
                layerDepth: 0.9f
            );
            
            e.SpriteBatch.End();
            e.SpriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp
            );
            //
            // e.SpriteBatch.Draw(
            //     texture: Game1.staminaRect,
            //     destinationRectangle: e.SpriteBatch.GraphicsDevice.Viewport.Bounds,
            //     sourceRectangle: null,
            //     color: Color.White * 0f,
            //     rotation: 0f,
            //     origin: Vector2.Zero,
            //     effects: SpriteEffects.None,
            //     layerDepth: 0.9f
            // );
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            // if (e.Button.IsUseToolButton())
            // {
            //     Log.Warn("Pressed use tool button.");
            //     if (Game1.player.ActiveObject is not null && Game1.player.ActiveObject.QualifiedItemId.Equals($"(O){UNIQUE_ID}_Agrometer"))
            //     {
            //         Game1.activeClickableMenu = new AgrometerMenu();
            //     }
            // }

            if (e.Button is SButton.F8)
            {
                Log.Warn($"Current Essences on held item:");
                if (Game1.player.ActiveObject is not null &&
                    Game1.player.ActiveObject.modData.ContainsKey(Manifest.UniqueID))
                {
                    CropEssences essences =
                        JsonConvert.DeserializeObject<CropEssences>(
                            Game1.player.ActiveObject.modData[Manifest.UniqueID]!)!;
                    foreach (var prop in typeof(CropEssences).GetProperties())
                    {
                        if (prop.PropertyType == typeof(byte[]))
                        {
                            byte[] arr = (byte[])prop.GetValue(essences)!;
                            Log.Info($"{prop.Name}: [{string.Join(", ", arr)}]");
                        }
                        else Log.Info($"{prop.Name}: {prop.GetValue(essences)}");
                    }
                }

                Game1.player.ActiveObject?.ApplyEssences(EssenceCalculator.RandomEssences());
            }

            if (e.Button is SButton.F5 && Game1.player.ActiveObject is not null)
            {
                Game1.player.ActiveObject.modData[Manifest.UniqueID] = JsonConvert.SerializeObject(new CropEssences
                {
                    YieldEssence = 255,
                    QualityEssence = [255, 255, 255],
                    GrowthEssence = 255,
                    GiantEssence = 255,
                    WaterEssence = 255,
                    SeedEssence = 255
                });
                Log.Info("Added Agromancy data to held item.");
            }

            if (e.Button is SButton.F3)
            {
                var clickedTile = e.Cursor.Tile;
                if (Game1.currentLocation.terrainFeatures.TryGetValue(clickedTile, out var terrainFeature))
                {
                    if (terrainFeature is not HoeDirt feature) return;
                    CropEssences? hoeDirtEssences = CropManager.GrabEssences(feature.crop);
                    Log.Error("------------------------");
                    if (hoeDirtEssences is null)
                    {
                        Log.Warn("No Agromancy data found on this crop.");
                        return;
                    }

                    Log.Info("Agromancy data found on this crop:");

                    foreach (var prop in typeof(CropEssences).GetProperties())
                    {
                        if (prop.PropertyType == typeof(byte[]))
                        {
                            byte[] arr = (byte[])prop.GetValue(hoeDirtEssences)!;
                            Log.Info($"{prop.Name}: [{string.Join(", ", arr)}]");
                        }
                        else Log.Info($"{prop.Name}: {prop.GetValue(hoeDirtEssences)}");
                    }
                }
                else if (Game1.player.ActiveObject is not null)
                {
                    bool hasAgroData = Game1.player.ActiveObject.modData.ContainsKey(Manifest.UniqueID);
                    Log.Error("------------------------");
                    Log.Warn($"Has Agromancy Data: {hasAgroData}");
                    if (hasAgroData)
                    {
                        CropEssences essences =
                            JsonConvert.DeserializeObject<CropEssences>(
                                Game1.player.ActiveObject.modData[Manifest.UniqueID]!)!;
                        foreach (var prop in typeof(CropEssences).GetProperties())
                        {
                            if (prop.PropertyType == typeof(byte[]))
                            {
                                byte[] arr = (byte[])prop.GetValue(essences)!;
                                Log.Info($"{prop.Name}: [{string.Join(", ", arr)}]");
                            }
                            else Log.Info($"{prop.Name}: {prop.GetValue(essences)}");
                        }
                    }
                }
            }
        }
    }
}