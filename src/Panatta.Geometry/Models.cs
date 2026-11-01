namespace Panatta.Geometry;

public sealed record Vertex(
    [property: System.Text.Json.Serialization.JsonRequired] double E,
    [property: System.Text.Json.Serialization.JsonRequired] double N,
    double Bulge = 0, double Z = 0);
public sealed record BoundaryInput
{
    public string SchemaVersion { get; init; } = "1.0";
    public string Source { get; init; } = "manual";
    public string? Drawing { get; init; }
    public string? Handle { get; init; }
    public string? CoordinateReference { get; init; }
    public string Units { get; init; } = "";
    public bool Closed { get; init; }
    public int StartVertex { get; init; } = 1;
    public bool Reverse { get; init; }
    public double? NativeArea { get; init; }
    public double? NativePerimeter { get; init; }
    public double LinearToleranceM { get; init; } = 1e-6;
    public double AreaToleranceM2 { get; init; } = 1e-4;
    public List<Vertex> Vertices { get; init; } = [];
}

public sealed record Issue(string Code, string Severity, string Message, int? Segment = null);
public sealed record SegmentResult(
    int Number, int SourceVertex, int NextSourceVertex, string Type,
    double StartE, double StartN, double EndE, double EndN,
    double LengthM, double ChordM, double ChordAzimuthDegrees,
    double? RadiusM, double? CenterE, double? CenterN,
    double? CentralAngleDegrees, string? CurveDirection,
    string ExteriorOrientation, double ExteriorNormalAzimuthDegrees);

public sealed record AnalysisResult
{
    public string SchemaVersion { get; init; } = "1.0";
    public string Status { get; init; } = "invalid";
    public string Purpose { get; init; } = "FASE 1 - CONFERENCIA GEOMETRICA; NAO E MEMORIAL DEFINITIVO";
    public string CalculationUnits { get; init; } = "m";
    public string NorthReference { get; init; } = "Eixo +N do WCS; referencia geodesica nao verificada";
    public string? Winding { get; init; }
    public double? AreaM2 { get; init; }
    public double? PerimeterM { get; init; }
    public double? ElevationM { get; init; }
    public double? NativeAreaDifferenceM2 { get; init; }
    public double? NativePerimeterDifferenceM { get; init; }
    public List<SegmentResult> Segments { get; init; } = [];
    public List<Issue> Issues { get; init; } = [];
}
