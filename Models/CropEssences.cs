namespace Agromancy.Models;

public class CropEssences
{
    public byte YieldEssence { get; set; }
    public byte QualityEssence { get; set; }
    public byte GrowthEssence { get; set; }
    public byte GiantEssence { get; set; }
    public byte WaterEssence { get; set; }
    public byte SeedEssence { get; set; }
    
    public override string ToString()
    {
        return "{" +
               $"YieldEssence: {YieldEssence}, " +
               $"QualityEssence: {QualityEssence}, " +
               $"GrowthEssence: {GrowthEssence}, " +
               $"GiantEssence: {GiantEssence}, " +
               $"WaterEssence: {WaterEssence}, " +
               $"SeedEssence: {SeedEssence}" +
               "}";
    }
}