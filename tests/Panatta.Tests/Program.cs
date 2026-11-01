using Panatta.Geometry;

int passed = 0, failed = 0;
void Test(string name, Action run)
{
    try { run(); passed++; Console.WriteLine("PASS " + name); }
    catch (Exception e) { failed++; Console.WriteLine("FAIL " + name + ": " + e.Message); }
}
void Equal(double? actual, double expected, double eps = 1e-6)
{
    if (!actual.HasValue || Math.Abs(actual.Value - expected) > eps) throw new Exception($"expected {expected:R}, found {actual:R}");
}
void True(bool condition, string reason) { if (!condition) throw new Exception(reason); }
BoundaryInput Shape(params Vertex[] vertices) => new() { Units = "m", Closed = true, Vertices = [.. vertices] };
BoundaryInput Rectangle() => Shape(new(0, 0), new(10, 0), new(10, 20), new(0, 20));
void Valid(AnalysisResult r) => True(r.Status == "geometry_checked", string.Join("; ", r.Issues.Select(i => i.Code)));
void Invalid(BoundaryInput input, string code)
{
    var r = Analyzer.Analyze(input);
    True(r.Status == "invalid" && r.Issues.Any(i => i.Code == code), "missing diagnostic " + code);
}

Test("rectangle analytic area and perimeter", () => { var r = Analyzer.Analyze(Rectangle()); Valid(r); Equal(r.AreaM2, 200); Equal(r.PerimeterM, 60); });
Test("azimuth WCS and exterior normal differ", () => { var s = Analyzer.Analyze(Rectangle()).Segments[0]; Equal(s.ChordAzimuthDegrees, 90); True(s.ExteriorOrientation == "SUL", "normal must face south"); });
Test("semicircle plus diameter", () => { var r = Analyzer.Analyze(Shape(new(0, 0, 1), new(20, 0))); Valid(r); Equal(r.AreaM2, 50 * Math.PI); Equal(r.PerimeterM, 20 + 10 * Math.PI); Equal(r.Segments[0].RadiusM, 10); });
Test("negative bulge semicircle", () => { var r = Analyzer.Analyze(Shape(new(0, 0, -1), new(20, 0))); Valid(r); Equal(r.AreaM2, 50 * Math.PI); True(r.Segments[0].CurveDirection == "horario", "wrong direction"); });
Test("two semicircles form a full circle", () => { var r = Analyzer.Analyze(Shape(new(0, 0, 1), new(20, 0, 1))); Valid(r); Equal(r.AreaM2, 100 * Math.PI); Equal(r.PerimeterM, 20 * Math.PI); });
Test("major 270 degree arc and chord", () =>
{
    var r = Analyzer.Analyze(Shape(new(10, 0, Math.Tan(3 * Math.PI / 8)), new(0, -10)));
    Valid(r); Equal(r.AreaM2, 50 * (1.5 * Math.PI + 1)); Equal(r.PerimeterM, 15 * Math.PI + Math.Sqrt(200)); Equal(r.Segments[0].CentralAngleDegrees, 270);
});
Test("reverse arc flips direction, preserves invariants", () =>
{
    var x = Shape(new(0, 0, 1), new(20, 0)); var a = Analyzer.Analyze(x); var b = Analyzer.Analyze(x with { Reverse = true });
    Valid(b); Equal(b.AreaM2, a.AreaM2!.Value); Equal(b.PerimeterM, a.PerimeterM!.Value); True(b.Winding != a.Winding, "winding unchanged");
    True(b.Segments[1].CurveDirection == "horario", "bulge not reversed");
    True(b.Segments[1].ExteriorOrientation == a.Segments[0].ExteriorOrientation, "physical normal changed");
});
Test("start rotation preserves coverage and closing vertex", () =>
{
    var r = Analyzer.Analyze(Rectangle() with { StartVertex = 3 }); Valid(r);
    True(r.Segments.Select(s => s.SourceVertex).SequenceEqual(new[] { 3, 4, 1, 2 }), "wrong ordering");
    True(r.Segments[^1].NextSourceVertex == 3, "not closed"); Equal(r.AreaM2, 200);
});
Test("clockwise normal invariant", () =>
{
    var r = Analyzer.Analyze(Rectangle() with { Reverse = true }); Valid(r); Equal(r.AreaM2, 200); Equal(r.PerimeterM, 60);
    True(r.Segments.Single(s => s.SourceVertex == 2).ExteriorOrientation == "SUL", "wrong exterior side");
});
Test("concave L shape analytic", () => { var r = Analyzer.Analyze(Shape(new(0, 0), new(4, 0), new(4, 1), new(1, 1), new(1, 4), new(0, 4))); Valid(r); Equal(r.AreaM2, 7); Equal(r.PerimeterM, 16); });
Test("UTM translation invariance", () =>
{
    var x = Rectangle(); var r = Analyzer.Analyze(x with { Vertices = x.Vertices.Select(v => v with { E = v.E + 769760.0358, N = v.N + 7247246.0317 }).ToList() });
    Valid(r); Equal(r.AreaM2, 200); Equal(r.Segments[0].StartE, 769760.0358);
});
Test("rotation area and azimuth", () =>
{
    var x = Rectangle(); double c = Math.Cos(.7), s = Math.Sin(.7);
    var r = Analyzer.Analyze(x with { Vertices = x.Vertices.Select(v => new Vertex(c * v.E - s * v.N, s * v.E + c * v.N)).ToList() });
    Valid(r); Equal(r.AreaM2, 200); Equal(r.PerimeterM, 60); Equal(r.Segments[0].ChordAzimuthDegrees, 90 - .7 * 180 / Math.PI);
});
Test("mm scale, native comparison and elevation", () =>
{
    var x = Rectangle(); var r = Analyzer.Analyze(x with { Units = "mm", Vertices = x.Vertices.Select(v => new Vertex(v.E * 1000, v.N * 1000, 0, 2500)).ToList(), NativeArea = 200_000_000, NativePerimeter = 60000 });
    Valid(r); Equal(r.AreaM2, 200); Equal(r.PerimeterM, 60); Equal(r.ElevationM, 2.5); Equal(r.NativeAreaDifferenceM2, 0);
});
Test("international foot and US survey foot are distinct", () => { Equal(Analyzer.UnitScale("ft"), .3048, 1e-12); Equal(Analyzer.UnitScale("us-ft"), 1200.0 / 3937, 1e-12); True(Analyzer.UnitScale("ft") != Analyzer.UnitScale("us-ft"), "feet conflated"); });
Test("small-angle segment area is stable", () =>
{
    const double b = 1e-6, chord = 1000;
    var r = Analyzer.Analyze(Shape(new(0, 0, b), new(chord, 0))); Valid(r);
    // Independent leading-order series for circular segment: c²*b/3 + O(b³).
    Equal(r.AreaM2, chord * chord * b / 3, 1e-9);
});
Test("native area mismatch blocks checked status", () => Invalid(Rectangle() with { NativeArea = 201 }, "AREA_MISMATCH"));
Test("native perimeter mismatch", () => Invalid(Rectangle() with { NativePerimeter = 59 }, "PERIMETER_MISMATCH"));
Test("negative native area rejected", () => Invalid(Rectangle() with { NativeArea = -200 }, "NATIVE"));
Test("open boundary rejected without auto close", () => Invalid(Rectangle() with { Closed = false }, "OPEN"));
Test("bowtie self intersection rejected", () => Invalid(Shape(new(0, 0), new(4, 4), new(0, 4), new(4, 0)), "SELF_INTERSECTION"));
Test("nonadjacent touching rejected", () => Invalid(Shape(new(0, 0), new(4, 0), new(2, 2), new(4, 4), new(0, 4), new(2, 2)), "SELF_INTERSECTION"));
Test("overlapping collinear edges rejected", () => Invalid(Shape(new(0, 0), new(4, 0), new(2, 0), new(2, 3), new(0, 3)), "SELF_INTERSECTION"));
Test("overlapping coincident arcs rejected", () => Invalid(Shape(new(0, 0, 1), new(20, 0, -1)), "SELF_INTERSECTION"));
Test("line crossing arc interior rejected", () => Invalid(Shape(new(0, 0, 1), new(20, 0), new(10, -20)), "SELF_INTERSECTION"));
Test("duplicate vertex rejected", () => Invalid(Shape(new(0, 0), new(10, 0), new(10, 0), new(0, 20)), "DEGENERATE"));
Test("zero area rejected", () => Invalid(Shape(new(0, 0), new(10, 0), new(20, 0)), "SELF_INTERSECTION"));
Test("nonplanar rejected", () => Invalid(Shape(new(0, 0), new(10, 0, 0, 1), new(0, 10)), "NONPLANAR"));
Test("nonfinite coordinate rejected", () => Invalid(Shape(new(double.NaN, 0), new(10, 0), new(0, 10)), "NONFINITE"));
Test("nonfinite bulge rejected", () => Invalid(Shape(new(0, 0, double.PositiveInfinity), new(10, 0)), "NONFINITE"));
Test("unknown unit rejected", () => Invalid(Rectangle() with { Units = "" }, "UNITS"));
Test("invalid start rejected", () => Invalid(Rectangle() with { StartVertex = 0 }, "START"));
Test("invalid tolerance rejected", () => Invalid(Rectangle() with { LinearToleranceM = 0 }, "TOLERANCE"));
Test("unknown schema rejected", () => Invalid(Rectangle() with { SchemaVersion = "9" }, "SCHEMA"));
Test("no mutation of input", () =>
{
    var x = Shape(new(0, 0, 1), new(20, 0)); var copy = x.Vertices.ToArray();
    Analyzer.Analyze(x with { Reverse = true }); True(x.Vertices.SequenceEqual(copy), "input changed");
});
Test("random rectangle invariants, 100 deterministic cases", () =>
{
    var rng = new Random(12020);
    for (int i = 0; i < 100; i++)
    {
        double w = 1 + rng.NextDouble() * 100, h = 1 + rng.NextDouble() * 100;
        var x = Shape(new(0, 0), new(w, 0), new(w, h), new(0, h));
        var r = Analyzer.Analyze(x with { Reverse = i % 2 == 0, StartVertex = 1 + i % 4 });
        Valid(r); Equal(r.AreaM2, w * h); Equal(r.PerimeterM, 2 * (w + h));
    }
});
Test("segmented circles, analytic area and perimeter, 60 cases", () =>
{
    for (int n = 2; n <= 7; n++)
    for (int trial = 0; trial < 10; trial++)
    {
        double radius = 10 + trial * 13.7, sweep = 2 * Math.PI / n;
        var vertices = Enumerable.Range(0, n).Select(i => new Vertex(
            770000 + radius * Math.Cos(i * sweep), 7247000 + radius * Math.Sin(i * sweep), Math.Tan(sweep / 4))).ToArray();
        var r = Analyzer.Analyze(Shape(vertices) with { Reverse = trial % 2 == 0, StartVertex = trial % n + 1 });
        Valid(r); Equal(r.AreaM2, Math.PI * radius * radius, 1e-4); Equal(r.PerimeterM, 2 * Math.PI * radius, 1e-6);
    }
});
Console.WriteLine($"\n{passed} passed; {failed} failed. No skipped tests.");
return failed == 0 ? 0 : 1;
