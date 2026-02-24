namespace Stafr.Models;

public sealed record PatternMetadata(
    string Name,
    FabricMetadata Fabric,
    int Complexity
);

public sealed record FabricMetadata(
    double Meters,
    int WidthCm
);