namespace Stafr.Models;

public sealed class PatternMetadata
{
    public string Name { get; set; } = "";
    public FabricMetadata Fabric { get; set; } = new FabricMetadata();
    public int Complexity { get; set; }
};

public sealed class FabricMetadata
{
    public double Meters { get; set; } = 0;
    public int WidthCm { get; set; } = 0;
};