namespace Agromancy.Models;

public class CropEssences
{
    public byte YieldEssence { get; set; }
    public byte[] QualityEssence { get; set; } = new byte[3];
    public byte GrowthEssence { get; set; }
    public byte GiantEssence { get; set; }
    public byte WaterEssence { get; set; }
    public byte SeedEssence { get; set; }
}