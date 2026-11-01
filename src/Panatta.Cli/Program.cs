using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Panatta.Geometry;

return Run(args);

static int Run(string[] args)
{
    if (args.Length == 1 && args[0] == "--help")
    {
        Console.WriteLine("Panatta.Cli --input contorno.json --output pasta/prefixo [--coordinate-decimals 4] [--distance-decimals 3] [--area-decimals 2] [--angle-decimals 2]");
        return 0;
    }
    try
    {
        var options = new Dictionary<string, string>();
        var known = new[] { "--input", "--output", "--coordinate-decimals", "--distance-decimals", "--area-decimals", "--angle-decimals" };
        for (int i = 0; i < args.Length; i += 2)
        {
            if (!known.Contains(args[i]) || i + 1 >= args.Length || !options.TryAdd(args[i], args[i + 1])) throw new ArgumentException("Argumentos desconhecidos, duplicados ou incompletos. Use --help.");
        }
        int Decimals(string key, int fallback)
        {
            int n = options.TryGetValue(key, out var value) ? int.Parse(value, CultureInfo.InvariantCulture) : fallback;
            if (n < 0 || n > 8) throw new ArgumentException("Casas decimais: intervalo 0 a 8.");
            return n;
        }
        int coord = Decimals("--coordinate-decimals", 4), distance = Decimals("--distance-decimals", 3), area = Decimals("--area-decimals", 2), angle = Decimals("--angle-decimals", 2);
        if (!options.TryGetValue("--input", out var inputPath) || !options.TryGetValue("--output", out var prefix)) throw new ArgumentException("Informe --input e --output.");
        inputPath = Path.GetFullPath(inputPath); prefix = Path.GetFullPath(prefix);
        if (new FileInfo(inputPath).Length > 10_000_000) throw new ArgumentException("Arquivo excede 10 MB.");
        var bytes = File.ReadAllBytes(inputPath);
        var json = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
        var input = JsonSerializer.Deserialize<BoundaryInput>(bytes, json) ?? throw new ArgumentException("Entrada JSON nula.");
        if (input.Vertices is null || input.Units is null) throw new ArgumentException("Vertices/unidades nao podem ser nulos.");
        var numbers = input.Vertices.Where(v => v is not null).SelectMany(v => new[] { v.E, v.N, v.Z, v.Bulge })
            .Concat(new[] { input.LinearToleranceM, input.AreaToleranceM2, input.NativeArea ?? 0, input.NativePerimeter ?? 0 });
        if (numbers.Any(v => !double.IsFinite(v))) throw new ArgumentException("Entrada numerica nao finita/fora do intervalo; JSON nao pode conter infinito ou NaN.");
        string[] targets = [prefix + ".result.json", prefix + ".segments.csv", prefix + ".report.txt"];
        if (targets.Any(File.Exists) || targets.Contains(inputPath, OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)) throw new IOException("Saida existente ou igual a entrada. Escolha outro prefixo; nada foi sobrescrito.");
        var result = Analyzer.Analyze(input);
        var audit = new
        {
            schemaVersion = "1.0", engineVersion = "0.1.0", generatedAtUtc = DateTimeOffset.UtcNow,
            inputSha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
            input, presentation = new { coordinateDecimals = coord, distanceDecimals = distance, areaDecimals = area, angleDecimals = angle },
            result
        };
        string F(double? v, int digits) => v?.ToString("F" + digits, CultureInfo.GetCultureInfo("pt-BR")) ?? "-";
        string Dms(double degrees)
        {
            double seconds = Math.Round(degrees * 3600, angle, MidpointRounding.AwayFromZero);
            if (seconds >= 360 * 3600) seconds = 0;
            int deg = (int)(seconds / 3600), min = (int)((seconds - deg * 3600) / 60);
            return $"{deg:000}°{min:00}′{F(seconds - deg * 3600 - min * 60, angle)}″";
        }
        var report = new StringBuilder();
        report.AppendLine("PANATTA MEMORIAL - FASE 1 - CONFERENCIA GEOMETRICA");
        report.AppendLine("RASCUNHO TECNICO - NAO E MEMORIAL DEFINITIVO");
        report.AppendLine($"Estado: {result.Status} | Origem: {input.Source} | Handle: {input.Handle}");
        report.AppendLine($"Unidade de entrada: {input.Units}; resultados em metros e metros quadrados.");
        report.AppendLine($"Area: {F(result.AreaM2, area)} m² | Perimetro: {F(result.PerimeterM, distance)} m | Sentido: {result.Winding}");
        report.AppendLine($"Elevacao constante: {F(result.ElevationM, coord)} m (nao participa do calculo planimetrico).");
        report.AppendLine($"Diferenca area CAD: {F(result.NativeAreaDifferenceM2, 8)} m²; perimetro CAD: {F(result.NativePerimeterDifferenceM, 8)} m.");
        report.AppendLine(result.NorthReference);
        foreach (var s in result.Segments)
        {
            report.AppendLine($"\nTrecho {s.Number}: vertice original {s.SourceVertex} -> {s.NextSourceVertex} | {s.Type}");
            report.AppendLine($"Inicio E={F(s.StartE, coord)} N={F(s.StartN, coord)}; fim E={F(s.EndE, coord)} N={F(s.EndN, coord)} m.");
            report.AppendLine($"{(s.Type == "arc" ? "Desenvolvimento" : "Distancia")}: {F(s.LengthM, distance)} m; azimute {(s.Type == "arc" ? "da corda" : "do trecho")}: {Dms(s.ChordAzimuthDegrees)}.");
            report.AppendLine($"Orientacao exterior representativa: {s.ExteriorOrientation}; normal={Dms(s.ExteriorNormalAzimuthDegrees)}.");
            if (s.Type == "arc") report.AppendLine($"Raio={F(s.RadiusM, distance)} m; corda={F(s.ChordM, distance)} m; angulo central={F(s.CentralAngleDegrees, angle)}°; sentido={s.CurveDirection}; centro E={F(s.CenterE, coord)} N={F(s.CenterN, coord)} m.");
        }
        report.AppendLine("\nINCONSISTENCIAS E AVISOS:");
        foreach (var issue in result.Issues) report.AppendLine($"{issue.Severity} | {issue.Code} | trecho {issue.Segment?.ToString() ?? "-"} | {issue.Message}");
        var csv = new StringBuilder("segment,source_vertex,next_source_vertex,type,start_e_m,start_n_m,end_e_m,end_n_m,length_m,chord_m,chord_azimuth_deg,radius_m,center_e_m,center_n_m,central_angle_deg,curve_direction,exterior_orientation,exterior_normal_azimuth_deg\n");
        string N(double? value) => value?.ToString("R", CultureInfo.InvariantCulture) ?? "";
        foreach (var s in result.Segments) csv.AppendLine(string.Join(",", s.Number, s.SourceVertex, s.NextSourceVertex, s.Type,
            N(s.StartE), N(s.StartN), N(s.EndE), N(s.EndN), N(s.LengthM), N(s.ChordM), N(s.ChordAzimuthDegrees), N(s.RadiusM), N(s.CenterE), N(s.CenterN), N(s.CentralAngleDegrees), s.CurveDirection ?? "", s.ExteriorOrientation, N(s.ExteriorNormalAzimuthDegrees)));
        Directory.CreateDirectory(Path.GetDirectoryName(prefix)!);
        // Stage all files first. JSON is the commit marker, moved last. On failure remove only our outputs.
        var temps = new List<string>(); var created = new List<string>();
        try
        {
            string[] contents = [JsonSerializer.Serialize(audit, json), csv.ToString(), report.ToString()];
            for (int i = 0; i < targets.Length; i++)
            {
                string temp = targets[i] + "." + Guid.NewGuid().ToString("N") + ".tmp";
                temps.Add(temp); File.WriteAllText(temp, contents[i], new UTF8Encoding(false));
            }
            foreach (int i in new[] { 1, 2, 0 }) { File.Move(temps[i], targets[i], false); created.Add(targets[i]); }
        }
        catch { foreach (var file in created) File.Delete(file); throw; }
        finally { foreach (var file in temps) if (File.Exists(file)) File.Delete(file); }
        Console.WriteLine($"{result.Status}: {targets[2]}");
        return result.Status == "geometry_checked" ? 0 : 2;
    }
    catch (Exception e) when (e is ArgumentException or IOException or UnauthorizedAccessException or JsonException or FormatException or OverflowException)
    {
        Console.Error.WriteLine("PANATTA: " + e.Message);
        return 3;
    }
}
