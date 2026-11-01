namespace Panatta.Geometry;

public static class Analyzer
{
    public static double UnitScale(string units) => units switch
    {
        "m" => 1, "mm" => .001, "cm" => .01, "ft" => .3048, "in" => .0254,
        "us-ft" => 1200.0 / 3937, _ => throw new ArgumentException("Unidade nao confirmada/suportada: m, mm, cm, ft, in, us-ft.")
    };
    private static bool Finite(double x) => double.IsFinite(x);
    public static AnalysisResult Analyze(BoundaryInput input)
    {
        var issues = new List<Issue>();
        void Error(string code, string message, int? index = null) => issues.Add(new(code, "error", message, index));
        AnalysisResult Invalid() => new() { Issues = issues };
        if (input.SchemaVersion != "1.0") Error("SCHEMA", "Versao de dados nao suportada.");
        double scale;
        try { scale = UnitScale(input.Units); } catch (ArgumentException e) { Error("UNITS", e.Message); return Invalid(); }
        if (!input.Closed) Error("OPEN", "Polilinha aberta; nao foi fechada automaticamente.");
        int count = input.Vertices.Count;
        if (count < 2) Error("VERTICES", "Sao necessarios pelo menos dois vertices distintos (contorno com arco).");
        if (count > 10000) Error("SIZE", "Limite do prototipo: 10.000 vertices. Validacao de intersecoes e quadratica.");
        if (input.StartVertex < 1 || input.StartVertex > count) Error("START", "Vertice inicial fora do intervalo.");
        if (!Finite(input.LinearToleranceM) || input.LinearToleranceM <= 0 || !Finite(input.AreaToleranceM2) || input.AreaToleranceM2 <= 0)
            Error("TOLERANCE", "Tolerancias devem ser finitas e positivas.");
        if (input.Vertices.Any(v => v is null || !Finite(v.E) || !Finite(v.N) || !Finite(v.Z) || !Finite(v.Bulge))) Error("NONFINITE", "Coordenadas/bulges devem ser finitos.");
        if (input.NativeArea is double na && (!Finite(na) || na < 0) || input.NativePerimeter is double np && (!Finite(np) || np < 0))
            Error("NATIVE", "Area/perimetro de referencia invalidos.");
        if (issues.Count > 0) return Invalid();
        double eps = input.LinearToleranceM;
        if (input.Vertices.Any(v => Math.Abs((v.Z - input.Vertices[0].Z) * scale) > eps))
        { Error("NONPLANAR", "Contorno nao esta em plano paralelo a XY com elevacao constante."); return Invalid(); }
        var origin = new Point(input.Vertices[0].E * scale, input.Vertices[0].N * scale);
        var points = new List<Point>(); var ids = new List<int>(); var bulges = new List<double>();
        for (int i = 0; i < count; i++)
        {
            int index = (input.StartVertex - 1 + (input.Reverse ? -i : i) + count) % count;
            var v = input.Vertices[index];
            // Subtract before scaling for better relative-coordinate precision.
            points.Add(new((v.E - input.Vertices[0].E) * scale, (v.N - input.Vertices[0].N) * scale));
            ids.Add(index + 1);
            bulges.Add(input.Reverse ? -input.Vertices[(index - 1 + count) % count].Bulge : v.Bulge);
        }
        var primitives = new List<Primitive>();
        for (int i = 0; i < count; i++)
        {
            var p = new Primitive(points[i], points[(i + 1) % count], bulges[i]);
            if (p.Chord <= eps) Error("DEGENERATE", "Segmento nulo/menor que tolerancia; nao foi removido.", i + 1);
            if (!Finite(p.Length) || !Finite(p.SignedArea) || (p.Arc && (!Finite(p.Center.X) || !Finite(p.Center.Y) || p.Radius > 1e12)))
                Error("CURVE", "Curva fora do dominio numerico do prototipo.", i + 1);
            primitives.Add(p);
        }
        if (issues.Count > 0) return Invalid();
        for (int i = 0; i < count; i++)
        for (int j = i + 1; j < count; j++)
        {
            var (crossings, overlap) = Intersections.Find(primitives[i], primitives[j], eps);
            var allowed = new List<Point>();
            if (j == i + 1) allowed.Add(primitives[i].B);
            if (i == 0 && j == count - 1) allowed.Add(primitives[i].A);
            if (overlap || crossings.Any(p => !allowed.Any(q => (p - q).Norm <= eps)))
                Error("SELF_INTERSECTION", $"Intersecao/sobreposicao dos segmentos {i + 1} e {j + 1}.", i + 1);
        }
        double signedArea = primitives.Sum(p => p.SignedArea), perimeter = primitives.Sum(p => p.Length);
        if (!Finite(signedArea) || !Finite(perimeter)) Error("NUMERIC", "Resultado fora do dominio numerico.");
        if (Math.Abs(signedArea) <= input.AreaToleranceM2) Error("ZERO_AREA", "Area degenerada/menor que tolerancia.");
        if (issues.Count > 0) return Invalid();
        var segments = new List<SegmentResult>();
        string[] sectors = ["NORTE", "NORDESTE", "LESTE", "SUDESTE", "SUL", "SUDOESTE", "OESTE", "NOROESTE"];
        for (int i = 0; i < count; i++)
        {
            var p = primitives[i]; var tangent = p.TangentAtMidpoint;
            var outward = signedArea > 0 ? new Point(tangent.Y, -tangent.X) : new Point(-tangent.Y, tangent.X);
            double normalAz = Primitive.Azimuth(outward);
            var a = p.A + origin; var b = p.B + origin; var center = p.Center + origin;
            segments.Add(new(i + 1, ids[i], ids[(i + 1) % count], p.Arc ? "arc" : "line", a.X, a.Y, b.X, b.Y,
                p.Length, p.Chord, Primitive.Azimuth(p.B - p.A), p.Arc ? p.Radius : null,
                p.Arc ? center.X : null, p.Arc ? center.Y : null, p.Arc ? Math.Abs(p.Theta) * 180 / Math.PI : null,
                p.Arc ? (p.Theta > 0 ? "anti-horario" : "horario") : null,
                sectors[(int)Math.Floor((normalAz + 22.5) / 45) % 8], normalAz));
            if (p.Arc) issues.Add(new("CURVE_ORIENTATION", "warning", "Orientacao representativa pela normal no meio do arco; revisar variacao ao longo da curva.", i + 1));
        }
        double? areaDelta = input.NativeArea.HasValue ? Math.Abs(signedArea) - input.NativeArea.Value * scale * scale : null;
        double? perimeterDelta = input.NativePerimeter.HasValue ? perimeter - input.NativePerimeter.Value * scale : null;
        if (Math.Abs(areaDelta ?? 0) > input.AreaToleranceM2) Error("AREA_MISMATCH", "Area calculada diverge da referencia nativa.");
        if (Math.Abs(perimeterDelta ?? 0) > eps) Error("PERIMETER_MISMATCH", "Perimetro calculado diverge da referencia nativa.");
        if (!input.NativeArea.HasValue || !input.NativePerimeter.HasValue) issues.Add(new("NO_NATIVE_CHECK", "warning", "Comparacao CAD incompleta; faltam area/perimetro nativos."));
        issues.Add(new("NO_CRS_VERIFICATION", "warning", "Coordenadas WCS nao comprovam georreferenciamento; nenhum ajuste geodesico aplicado."));
        return new()
        {
            Status = issues.Any(x => x.Severity == "error") ? "invalid" : "geometry_checked",
            Winding = signedArea > 0 ? "anti-horario" : "horario", AreaM2 = Math.Abs(signedArea), PerimeterM = perimeter,
            ElevationM = input.Vertices[0].Z * scale, Segments = segments, Issues = issues,
            NativeAreaDifferenceM2 = areaDelta, NativePerimeterDifferenceM = perimeterDelta
        };
    }
}
