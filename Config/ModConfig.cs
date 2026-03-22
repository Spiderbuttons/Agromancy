using GenericModConfigMenu;
using StardewModdingAPI;

namespace Agromancy.Config;

public sealed class ModConfig
{
    public bool DrainedCropsLoseValue { get; set; } = true;
    public bool AllowCropMutations { get; set; } = true;
    public bool PositiveMutationsOnly { get; set; } = false;

    public ModConfig()
    {
        Init();
    }

    private void Init()
    {
        DrainedCropsLoseValue = true;
        AllowCropMutations = true;
        PositiveMutationsOnly = false;
    }

    public void SetupConfig(IGenericModConfigMenuApi configMenu, IManifest ModManifest, IModHelper Helper)
    {
        configMenu.Register(
            mod: ModManifest,
            reset: Init,
            save: () => Helper.WriteConfig(this)
        );
        
        configMenu.AddBoolOption(
            mod: ModManifest,
            name: i18n.Config_DrainedCropsLoseValueName,
            tooltip: i18n.Config_DrainedCropsLoseValueDescription,
            getValue: () => DrainedCropsLoseValue,
            setValue: value => DrainedCropsLoseValue = value
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: i18n.Config_AllowMutationsName,
            tooltip: i18n.Config_AllowMutationsDescription,
            getValue: () => AllowCropMutations,
            setValue: value => AllowCropMutations = value
        );
        
        configMenu.AddBoolOption(
            mod: ModManifest,
            name: i18n.Config_PositiveMutationsName,
            tooltip: i18n.Config_PositiveMutationsDescription,
            getValue: () => PositiveMutationsOnly,
            setValue: value => PositiveMutationsOnly = value
        );
    }
}