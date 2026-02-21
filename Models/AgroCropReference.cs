using StardewValley.GameData.Crops;
using StardewValley.GameData.GiantCrops;

namespace Agromancy.Models;

public class AgroCropReference(CropData cropData, GiantCropData? giantCropData)
{
    public CropData CropData { get; } = cropData;
    public GiantCropData? GiantCropData { get; } = giantCropData;
    
    public bool CanBeGiant => GiantCropData != null;
}