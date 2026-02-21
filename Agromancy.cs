using System;
using System.Collections.Generic;
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
using Agromancy.Models;
using Newtonsoft.Json;
using StardewValley.GameData.Crops;

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
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (e.Button is SButton.F8)
            {
                ModHelper.GameContent.InvalidateCache("Data/Crops");
            }

            if (e.Button is SButton.F3)
            {
                if (Game1.player.ActiveObject is not null)
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

                        Log.Alert("Mutating essences...");
                        essences.Mutate();
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