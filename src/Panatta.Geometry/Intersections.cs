namespace Panatta.Geometry;

internal static class Intersections
{
    // Analytic tests, including coincident curves and partial line overlaps.
    public static (List<Point> Points, bool Overlap) Find(Primitive a, Primitive b, double eps)
    {
        var points = new List<Point>();
        void Add(Point p) { if (a.Contains(p, eps) && b.Contains(p, eps) && !points.Any(q => (q - p).Norm <= eps)) points.Add(p); }
        if (!a.Arc && !b.Arc)
        {
            Point u = a.B - a.A, v = b.B - b.A;
            double cross = Point.Cross(u, v);
            if (Math.Abs(cross) <= 1e-14 * a.Chord * b.Chord)
            {
                if (Math.Abs(Point.Cross(u, b.A - a.A)) > eps * a.Chord) return (points, false);
                double t0 = Point.Dot(b.A - a.A, u) / Point.Dot(u, u), t1 = Point.Dot(b.B - a.A, u) / Point.Dot(u, u);
                double lo = Math.Max(0, Math.Min(t0, t1)), hi = Math.Min(1, Math.Max(t0, t1));
                if ((hi - lo) * a.Chord > eps) return (points, true);
                Add(a.A); Add(a.B); Add(b.A); Add(b.B);
            }
            else
            {
                double t = Point.Cross(b.A - a.A, v) / cross;
                Add(a.A + u * t);
            }
        }
        else if (a.Arc && b.Arc)
        {
            var delta = b.Center - a.Center; double d = delta.Norm;
            if (d <= eps && Math.Abs(a.Radius - b.Radius) <= eps)
            {
                // Subdivide by endpoints; test interior of each angular interval.
                var cuts = new List<double> { 0, Math.Abs(a.Theta) };
                foreach (var p in new[] { b.A, b.B })
                    if (a.Contains(p, eps))
                    {
                        double travel = (p - a.A).Norm <= eps ? 0 : (p - a.B).Norm <= eps ? Math.Abs(a.Theta)
                            : Primitive.Positive(Math.Sign(a.Theta) * (Math.Atan2(p.Y - a.Center.Y, p.X - a.Center.X) - a.StartAngle));
                        cuts.Add(Math.Clamp(travel, 0, Math.Abs(a.Theta)));
                    }
                cuts.Sort();
                for (int i = 0; i + 1 < cuts.Count; i++)
                {
                    if ((cuts[i + 1] - cuts[i]) * a.Radius <= eps) continue;
                    double angle = a.StartAngle + Math.Sign(a.Theta) * (cuts[i + 1] + cuts[i]) / 2;
                    Point mid = a.Center + new Point(Math.Cos(angle), Math.Sin(angle)) * a.Radius;
                    if (b.Contains(mid, eps)) return (points, true);
                }
                Add(a.A); Add(a.B); Add(b.A); Add(b.B);
            }
            else if (d > 0 && d <= a.Radius + b.Radius + eps && d >= Math.Abs(a.Radius - b.Radius) - eps)
            {
                double x = (a.Radius * a.Radius - b.Radius * b.Radius + d * d) / (2 * d);
                double h2 = a.Radius * a.Radius - x * x;
                if (h2 >= -eps * Math.Max(a.Radius, b.Radius))
                {
                    var middle = a.Center + delta * (x / d);
                    var offset = new Point(-delta.Y, delta.X) * (Math.Sqrt(Math.Max(0, h2)) / d);
                    Add(middle + offset); Add(middle - offset);
                }
            }
        }
        else
        {
            var line = a.Arc ? b : a; var arc = a.Arc ? a : b;
            var v = line.B - line.A; var unit = v * (1 / line.Chord);
            // Closest point on the infinite line; avoids unstable quadratic roots.
            double t = Point.Dot(arc.Center - line.A, unit);
            Point closest = line.A + unit * t;
            double h2 = arc.Radius * arc.Radius - Point.Dot(closest - arc.Center, closest - arc.Center);
            if (h2 >= -eps * arc.Radius)
            {
                var offset = unit * Math.Sqrt(Math.Max(0, h2));
                Add(closest + offset); Add(closest - offset);
            }
        }
        return (points, false);
    }
}
