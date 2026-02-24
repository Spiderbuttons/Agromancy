using System;
using System.Collections.Generic;
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
using StardewValley.GameData.Crops;
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
        
        /* Shaders */
        public static Effect BlendFx = null!;
        
        private static RenderTarget2D uiScreen = null;
        private static RenderTarget2D sceneScreen = null;
        
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
            
            try {
                byte[] stream = File.ReadAllBytes(Path.Combine(helper.DirectoryPath, "assets/fx/blend.mgfx"));
                BlendFx = new Effect(Game1.graphics.GraphicsDevice, stream);
            }
            catch (Exception e)
            {
                Log.Error("Unable to load blur shader: " + e);
            }

            Harmony.PatchAll();

            Helper.Events.Input.ButtonPressed += OnButtonPressed;
            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
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
            
            if (e.NameWithoutLocale.IsEquivalentTo($"{UNIQUE_ID}/AgrometerRing"))
            {
                e.LoadFromModFile<Texture2D>("assets/menuBG_Leafy.png", AssetLoadPriority.Exclusive);
            }
        }
        
        public void EnsureBuffers(RenderTarget2D worldSource, bool reallocate = false)
        {
            // we probably don't need to null coalesce here, but better safe
            // than sorry
            int sw = (worldSource ?? Game1.game1.screen).Width;
            int sh = (worldSource ?? Game1.game1.screen).Height;
            if (reallocate || sceneScreen is null || 
                (sceneScreen.Width != sw || sceneScreen.Height != sh)) {
                sceneScreen?.Dispose();
                sceneScreen = new(Game1.graphics.GraphicsDevice, sw, sh);
            }
            int uw = Game1.game1.uiScreen.Width;
            int uh = Game1.game1.uiScreen.Height;
            if (reallocate || uiScreen is null || 
                (uiScreen.Width != uw || uiScreen.Height != uh)) {
                uiScreen?.Dispose();
                uiScreen = new(Game1.graphics.GraphicsDevice, uw, uh);
            }
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (e.Button is SButton.F8)
            {
                if (Game1.activeClickableMenu is not null)
                {
                    Game1.activeClickableMenu = null;
                } else Game1.activeClickableMenu = new AgrometerMenu();
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
                        CropEssences essences = JsonConvert.DeserializeObject<CropEssences>(Game1.player.ActiveObject.modData[Manifest.UniqueID]!)!;
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