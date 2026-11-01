namespace Panatta.Geometry;

internal readonly record struct Point(double X, double Y)
{
    public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
    public static Point operator -(Point a, Point b) => new(a.X - b.X, a.Y - b.Y);
    public static Point operator *(Point a, double k) => new(a.X * k, a.Y * k);
    public double Norm => Math.Sqrt(X * X + Y * Y);
    public static double Cross(Point a, Point b) => a.X * b.Y - a.Y * b.X;
    public static double Dot(Point a, Point b) => a.X * b.X + a.Y * b.Y;
}

// Coordinates here are translated near the first vertex to avoid UTM cancellation.
internal sealed class Primitive
{
    public Point A { get; }
    public Point B { get; }
    public double Bulge { get; }
    public double Theta { get; }
    public Point Center { get; }
    public double Radius { get; }
    public bool Arc => Bulge != 0;
    public double Chord => (B - A).Norm;
    public double Length => Arc ? Radius * Math.Abs(Theta) : Chord;
    public Primitive(Point a, Point b, double bulge)
    {
        A = a; B = b; Bulge = bulge;
        if (!Arc) return;
        Theta = 4 * Math.Atan(bulge);
        var delta = b - a;
        // Algebraic form avoids squaring very large bulges.
        Radius = delta.Norm * (Math.Abs(bulge) + 1 / Math.Abs(bulge)) / 4;
        Center = (a + b) * .5 + new Point(-delta.Y, delta.X) * ((1 / bulge - bulge) / 4);
    }
    public static double Positive(double angle) => (angle % (2 * Math.PI) + 2 * Math.PI) % (2 * Math.PI);
    public static double Azimuth(Point vector) => Positive(Math.Atan2(vector.X, vector.Y)) * 180 / Math.PI;
    public double StartAngle => Math.Atan2(A.Y - Center.Y, A.X - Center.X);
    public Point Midpoint => Arc ? Center + new Point(Math.Cos(StartAngle + Theta / 2), Math.Sin(StartAngle + Theta / 2)) * Radius : (A + B) * .5;
    public Point TangentAtMidpoint => Arc ? new Point(-(Midpoint - Center).Y, (Midpoint - Center).X) * Math.Sign(Theta) : B - A;
    public bool Contains(Point p, double tolerance)
    {
        if (!Arc)
        {
            var v = B - A;
            return Math.Abs(Point.Cross(v, p - A)) <= tolerance * Chord &&
                Point.Dot(p - A, v) >= -tolerance * Chord && Point.Dot(p - B, v) <= tolerance * Chord;
        }
        if (Math.Abs((p - Center).Norm - Radius) > tolerance) return false;
        if ((p - A).Norm <= tolerance || (p - B).Norm <= tolerance) return true;
        double travel = Positive(Math.Sign(Theta) * (Math.Atan2(p.Y - Center.Y, p.X - Center.X) - StartAngle));
        return travel <= Math.Abs(Theta) + tolerance / Radius;
    }
    public double SignedArea
    {
        get
        {
            double t = Theta;
            // t - sin(t) loses significant digits near zero.
            double difference = Math.Abs(t) < .01 ? t * t * t / 6 * (1 - t * t / 20 + Math.Pow(t, 4) / 840 - Math.Pow(t, 6) / 60480) : t - Math.Sin(t);
            return Point.Cross(A, B) / 2 + (Arc ? Radius * Radius * difference / 2 : 0);
        }
    }
}
