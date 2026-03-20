using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Agromancy.APIs;
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
using Agromancy.Pedestals;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Buffs;
using StardewValley.GameData.Crops;
using StardewValley.GameData.GiantCrops;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Tools;
using StardewValley.Mods;
using StardewValley.Objects;
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
        
        internal static Texture2D PerlinNoise { get; set; } = null!;

        /* Shaders */
        public static Effect GrayscaleFx = null!;
        public static Effect BlurFx = null!;
        public static Effect StatsFx = null!;
        public static Effect LiquidCircleFx = null!;
        public static Effect DissolveFx = null!;
        public static Effect EssenceVialFx = null!;

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
                // TODO: Path.Combine
                byte[] stream = File.ReadAllBytes(Path.Combine(helper.DirectoryPath, "assets/shaders/grayscale.mgfx"));
                GrayscaleFx = new Effect(Game1.graphics.GraphicsDevice, stream);
                byte[] blurStream = File.ReadAllBytes(Path.Combine(helper.DirectoryPath, "assets/shaders/blur.mgfx"));
                BlurFx = new Effect(Game1.graphics.GraphicsDevice, blurStream);
                byte[] statsStream = File.ReadAllBytes(Path.Combine(helper.DirectoryPath, "assets/shaders/stats.mgfx"));
                StatsFx = new Effect(Game1.graphics.GraphicsDevice, statsStream);
                byte[] liquidCircleStream = File.ReadAllBytes(Path.Combine(helper.DirectoryPath, "assets/shaders/liquidcircle.mgfx"));
                LiquidCircleFx = new Effect(Game1.graphics.GraphicsDevice, liquidCircleStream);
                byte[] dissolveStream = File.ReadAllBytes(Path.Combine(helper.DirectoryPath, "assets/shaders/dissolve.mgfx"));
                DissolveFx = new Effect(Game1.graphics.GraphicsDevice, dissolveStream);
                byte[] essenceVialStream = File.ReadAllBytes(Path.Combine(helper.DirectoryPath, "assets/shaders/essencevial.mgfx"));
                EssenceVialFx = new Effect(Game1.graphics.GraphicsDevice, essenceVialStream);
            }
            catch (Exception e)
            {
                Log.Error("Error loading shaders: " + e);
            }

            Harmony.PatchAll();

            Helper.Events.Input.ButtonPressed += OnButtonPressed;
            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            Helper.Events.GameLoop.DayStarted += OnDayStarted;
            Helper.Events.Display.RenderedWorld += OnRenderedWorld;
            Helper.Events.Display.MenuChanged += OnMenuChanged;
            Helper.Events.Content.AssetRequested += OnAssetsRequested;
            Helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu != null) Config.SetupConfig(configMenu, ModManifest, Helper);

            var SCAPI = Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");
            if (SCAPI != null)
            {
                SCAPI.RegisterSerializerType(typeof(AgromanticPedestal));
                SCAPI.RegisterSerializerType(typeof(AgromanticAltar));
            }
            
            CropManager = new CropManager();
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            if (Game1.player.mailReceived.Contains($"{UNIQUE_ID}_FoundAgrometer"))
            {
                Game1.player.craftingRecipes.TryAdd($"{UNIQUE_ID}_Pedestal_Recipe", 0);
                Game1.player.craftingRecipes.TryAdd($"{UNIQUE_ID}_Altar_Recipe", 0);
            }
        }

        private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
        {
            if (!e.FromModID.Equals(UNIQUE_ID)) return;

            var info = e.ReadAs<RitualFinishInfo>();
            if (Game1.currentLocation is null || !Game1.currentLocation.NameOrUniqueName.Equals(info.Location)) return;
            var ped = Game1.currentLocation.getObjectAtTile((int)info.TilePosition.X, (int)info.TilePosition.Y);
            if (ped is AgromanticAltar altar)
            {
                altar.lightningStrike();
            }
        }

        private void OnAssetsRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, ObjectData>().Data;
                    data[$"{UNIQUE_ID}_T1EssenceVial"] = new ObjectData
                    {
                        Name = $"{UNIQUE_ID}_T1EssenceVial",
                        DisplayName = $"{TKString("Tier1")} {TKString("EssenceVial_Name")}",
                        Description = 
                            $"{TKString("EssenceVial_Description_T1")}\n\n" +
                            $"{TKString("ContainsEssence")}\n- " +
                            "{0} " + $"{TKString("Yield")}\n- " +
                            "{1} " + $"{TKString("Quality")}\n- " +
                            "{2} " + $"{TKString("Growth")}\n- " +
                            "{3} " + $"{TKString("Giant")}\n- " +
                            "{4} " + $"{TKString("Retention")}\n- " +
                            "{5} " + $"{TKString("Seed")}",
                        Type = "Basic",
                        Category = 0,
                        Price = 100,
                        Texture = $"{UNIQUE_ID}/Objects",
                        SpriteIndex = 0,
                    };
                    data[$"{UNIQUE_ID}_T2EssenceVial"] = new ObjectData
                    {
                        Name = $"{UNIQUE_ID}_T2EssenceVial",
                        DisplayName = $"{TKString("Tier2")} {TKString("EssenceVial_Name")}",
                        Description = 
                            $"{TKString("EssenceVial_Description_T2")}\n\n" +
                            $"{TKString("ContainsEssence")}\n- " +
                            "{0} " + $"{TKString("Yield")}\n- " +
                            "{1} " + $"{TKString("Quality")}\n- " +
                            "{2} " + $"{TKString("Growth")}\n- " +
                            "{3} " + $"{TKString("Giant")}\n- " +
                            "{4} " + $"{TKString("Retention")}\n- " +
                            "{5} " + $"{TKString("Seed")}",
                        Type = "Basic",
                        Category = 0,
                        Price = 100,
                        Texture = $"{UNIQUE_ID}/Objects",
                        SpriteIndex = 1,
                    };
                    data[$"{UNIQUE_ID}_T3EssenceVial"] = new ObjectData
                    {
                        Name = $"{UNIQUE_ID}_T3EssenceVial",
                        DisplayName = $"{TKString("Tier3")} {TKString("EssenceVial_Name")}",
                        Description = 
                            $"{TKString("EssenceVial_Description_T3")}\n\n" +
                            $"{TKString("ContainsEssence")}\n- " +
                            "{0} " + $"{TKString("Yield")}\n- " +
                            "{1} " + $"{TKString("Quality")}\n- " +
                            "{2} " + $"{TKString("Growth")}\n- " +
                            "{3} " + $"{TKString("Giant")}\n- " +
                            "{4} " + $"{TKString("Retention")}\n- " +
                            "{5} " + $"{TKString("Seed")}",
                        Type = "Basic",
                        Category = 0,
                        Price = 100,
                        Texture = $"{UNIQUE_ID}/Objects",
                        SpriteIndex = 2,
                    };
                });
            }

            if (e.NameWithoutLocale.IsEquivalentTo("Data/Tools"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, ToolData>().Data;
                    data[$"{UNIQUE_ID}_Agrometer"] = new ToolData
                    {
                        ClassName = "GenericTool",
                        Name = $"{UNIQUE_ID}_Agrometer",
                        DisplayName = TKString("Agrometer_Name"),
                        Description = TKString("Agrometer_Description"),
                        Texture = $"{UNIQUE_ID}/Objects",
                        SpriteIndex = 3,
                        AttachmentSlots = 1,
                        CanBeLostOnDeath = false,
                        SetProperties = new Dictionary<string, string>
                        {
                            { "InstantUse", "true" },
                            { "IsEfficient", "true" }
                        }
                    };
                });
            }

            if (e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, BigCraftableData>().Data;
                    data[$"{UNIQUE_ID}_Pedestal"] = new BigCraftableData
                    {
                        Name = $"{UNIQUE_ID}_Pedestal",
                        DisplayName = TKString("Pedestal_Name"),
                        Description = TKString("Pedestal_Description"),
                        Texture = $"{UNIQUE_ID}/Pedestals",
                        SpriteIndex = 2,
                    };
                    data[$"{UNIQUE_ID}_Altar"] = new BigCraftableData
                    {
                        Name = $"{UNIQUE_ID}_Altar",
                        DisplayName = TKString("Altar_Name"),
                        Description = TKString("Altar_Description"),
                        Texture = $"{UNIQUE_ID}/Pedestals",
                        SpriteIndex = 3,
                    };
                });
            }

            if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, string>().Data;
                    data[$"{UNIQUE_ID}_Pedestal_Recipe"] = $"1 1/Unused/1/true/Unused/{TKString("Pedestal_Name")}";
                    data[$"{UNIQUE_ID}_Altar_Recipe"] = $"1 1/Unused/1/true/Unused/{TKString("Altar_Name")}";
                });
            } else if (e.NameWithoutLocale.IsEquivalentTo("spacechase0.SpaceCore/CraftingRecipeOverrides"))
            {
                e.Edit(asset =>
                {
                    if (asset.Data is not IDictionary data) return;
                    var valueType = asset.DataType.GetGenericArguments()[1];
                    JObject pedRecipe = new JObject
                    {
                        ["Ingredients"] = new JArray
                        {
                            new JObject
                            {
                                ["Type"] = "Item",
                                ["Value"] = "(O)390",
                                ["Amount"] = 10
                            },
                            new JObject
                            {
                                ["Type"] = "ContextTag",
                                ["Value"] = "agromantic_seed",
                                ["Amount"] = 5,
                                ["ContextTagsRequireAll"] = true,
                                ["OverrideText"] = TKString("Seed"),
                                ["OverrideTexturePath"] = "Maps/springobjects",
                                ["OverrideTextureRect"] = new JObject
                                {
                                    ["X"] = 240,
                                    ["Y"] = 16,
                                    ["Width"] = 16,
                                    ["Height"] = 16,
                                }
                            }
                        },
                        ["ProductQualifiedId"] = $"(BC){UNIQUE_ID}_Pedestal",
                        ["ProductAmount"] = 1
                    };
                    JObject altarRecipe = new JObject
                    {
                        ["Ingredients"] = new JArray
                        {
                            new JObject
                            {
                                ["Type"] = "Item",
                                ["Value"] = "(O)390",
                                ["Amount"] = 10
                            },
                            new JObject
                            {
                                ["Type"] = "Item",
                                ["Value"] = "(O)771",
                                ["Amount"] = 5,
                            },
                            new JObject
                            {
                                ["Type"] = "ContextTag",
                                ["Value"] = "agromantic_crop, quality_gold",
                                ["Amount"] = 5,
                                ["ContextTagsRequireAll"] = true,
                                ["OverrideText"] = TKString("GoldCrops"),
                                ["OverrideTexturePath"] = "Maps/springobjects",
                                ["OverrideTextureRect"] = new JObject
                                {
                                    ["X"] = 144,
                                    ["Y"] = 240,
                                    ["Width"] = 16,
                                    ["Height"] = 16,
                                }
                            }
                        },
                        ["ProductQualifiedId"] = $"(BC){UNIQUE_ID}_Altar",
                        ["ProductAmount"] = 1
                    };
                    data[$"{UNIQUE_ID}_Pedestal_Recipe"] = pedRecipe.ToObject(valueType);
                    data[$"{UNIQUE_ID}_Altar_Recipe"] = altarRecipe.ToObject(valueType);
                });
            }

            if (e.NameWithoutLocale.IsEquivalentTo($"{UNIQUE_ID}/Strings"))
            {
                e.LoadFrom(() =>
                {
                    return ModHelper.ModContent.Load<Dictionary<string, string>>(Path.Combine("i18n", "default.json"));
                    
                    return new Dictionary<string, string>
                    {
                        ["Agromancy"] = i18n.Agromancy(),
                        ["Pedestal_Name"] = i18n.PedestalName(),
                        ["Pedestal_Description"] = i18n.PedestalDescription(),
                        ["Altar_Name"] = i18n.AltarName(),
                        ["Altar_Description"] = i18n.AltarDescription(),
                        ["Agrometer_Name"] = i18n.AgrometerName(),
                        ["Agrometer_Description"] = i18n.AgrometerDescription(),
                        ["Tier1"] = i18n.Tier1(),
                        ["Tier2"] = i18n.Tier2(),
                        ["Tier3"] = i18n.Tier3(),
                        ["EssenceVial_Name"] = i18n.EssenceVialName(),
                        ["EssenceVial_Description_T1"] = i18n.EssenceVialDescriptionT1(),
                        ["EssenceVial_Description_T2"] = i18n.EssenceVialDescriptionT2(),
                        ["EssenceVial_Description_T3"] = i18n.EssenceVialDescriptionT3(),
                        ["ContainsEssence"] = i18n.ContainsEssence(),
                        ["Yield"] = i18n.Yield(),
                        ["Quality"] = i18n.Quality(),
                        ["Growth"] = i18n.Growth(),
                        ["Giant"] = i18n.Giant(),
                        ["Retention"] = i18n.Retention(),
                        ["Seed"] = i18n.Seed(),
                    };
                }, AssetLoadPriority.Exclusive);
            }

            if (e.NameWithoutLocale.IsEquivalentTo($"{UNIQUE_ID}/Pedestals"))
            {
                e.LoadFromModFile<Texture2D>("assets/pedestals.png", AssetLoadPriority.Medium);
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
                e.LoadFromModFile<Texture2D>("assets/arrows.png", AssetLoadPriority.Medium);
            }
            
            if (e.NameWithoutLocale.IsEquivalentTo($"{UNIQUE_ID}/AllButton"))
            {
                e.LoadFromModFile<Texture2D>("assets/allButton.png", AssetLoadPriority.Medium);
            }

            if (e.NameWithoutLocale.IsEquivalentTo($"{UNIQUE_ID}/Objects"))
            {
                e.LoadFromModFile<Texture2D>("assets/objects.png", AssetLoadPriority.Exclusive);
            }

            if (e.NameWithoutLocale.IsEquivalentTo($"{UNIQUE_ID}/EssenceIcons"))
            {
                e.LoadFromModFile<Texture2D>("assets/essenceIcons.png", AssetLoadPriority.Medium);
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

        public static string TKString(string key)
        {
            return $"[LocalizedText {UNIQUE_ID}\\Strings:{key}]";
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (e.Button is SButton.F6)
            {
                // Game1.weatherForTomorrow = "Rain";
                // Game1.ApplyWeatherForNewDay();
            }

            if (e.Button is SButton.F8)
            {
                Game1.player.ActiveObject?.ApplyEssences(EssenceCalculator.RandomEssences());
                Log.Warn($"Random Essences on held item:");
                if (Game1.player.ActiveObject is not null &&
                    Game1.player.ActiveObject.modData.ContainsKey(Manifest.UniqueID))
                {
                    CropEssences essences = JsonConvert.DeserializeObject<CropEssences>(Game1.player.ActiveObject.modData[Manifest.UniqueID]!)!;
                    Log.Debug(essences);
                }
            }

            if (e.Button is SButton.F5 && Game1.player.ActiveObject is not null)
            {
                Game1.player.ActiveObject.modData[Manifest.UniqueID] = JsonConvert.SerializeObject(new CropEssences
                {
                    YieldEssence = 255,
                    QualityEssence = 255,
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
                if (Game1.currentLocation.resourceClumps.FirstOrDefault(c => c.occupiesTile((int)clickedTile.X, (int)clickedTile.Y)) is not null)
                {
                    var clump = Game1.currentLocation.resourceClumps.FirstOrDefault(c => c.occupiesTile((int)clickedTile.X, (int)clickedTile.Y));
                    if (clump is null) return;
                    CropEssences? clumpEssences = CropManager.GrabEssences(clump);
                    Log.Error("------------------------");
                    if (clumpEssences is null)
                    {
                        Log.Warn("No Agromancy data found on this resource clump.");
                        return;
                    }
                    Log.Info("Agromancy data found on this resource clump:");
                    foreach (var prop in typeof(CropEssences).GetProperties())
                    {
                        Log.Info($"{prop.Name}: {prop.GetValue(clumpEssences)}");
                    }
                }
                // else if (Game1.currentLocation.terrainFeatures.TryGetValue(clickedTile, out var terrainFeature))
                // {
                //     if (terrainFeature is not HoeDirt feature) return;
                //     CropEssences? hoeDirtEssences = CropManager.GrabEssences(feature.crop);
                //     Log.Error("------------------------");
                //     if (hoeDirtEssences is null)
                //     {
                //         Log.Warn("No Agromancy data found on this crop.");
                //         return;
                //     }
                //
                //     Log.Info("Agromancy data found on this crop:");
                //
                //     foreach (var prop in typeof(CropEssences).GetProperties())
                //     {
                //         if (prop.PropertyType == typeof(byte[]))
                //         {
                //             byte[] arr = (byte[])prop.GetValue(hoeDirtEssences)!;
                //             Log.Info($"{prop.Name}: [{string.Join(", ", arr)}]");
                //         }
                //         else Log.Info($"{prop.Name}: {prop.GetValue(hoeDirtEssences)}");
                //     }
                // }
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