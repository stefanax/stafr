using System;
using System.Collections.Generic;
using System.Linq;

namespace Stafr.Geometry;

// --- Layers ---------------------------------------------------------------

public enum LayerTag
{
    Cut,
    Engrave,
    Tool
}

// --- Math ----------------------------------------------------------------

public readonly record struct Vec2(double X, double Y)
{
    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.X - b.X, a.Y - b.Y);
}

public readonly record struct RectD(double MinX, double MinY, double MaxX, double MaxY)
{
    public double Width => MaxX - MinX;
    public double Height => MaxY - MinY;

    public static RectD Empty => new(double.PositiveInfinity, double.PositiveInfinity,
                                     double.NegativeInfinity, double.NegativeInfinity);

    public bool IsEmpty => double.IsPositiveInfinity(MinX);

    public RectD Include(Vec2 p) =>
        IsEmpty
            ? new RectD(p.X, p.Y, p.X, p.Y)
            : new RectD(Math.Min(MinX, p.X), Math.Min(MinY, p.Y),
                        Math.Max(MaxX, p.X), Math.Max(MaxY, p.Y));

    public RectD Union(RectD other)
    {
        if (IsEmpty) return other;
        if (other.IsEmpty) return this;
        return new RectD(
            Math.Min(MinX, other.MinX),
            Math.Min(MinY, other.MinY),
            Math.Max(MaxX, other.MaxX),
            Math.Max(MaxY, other.MaxY)
        );
    }
}

/// <summary>
/// 2D affine transform in the common SVG/LB form:
/// [ A C E ]
/// [ B D F ]
/// [ 0 0 1 ]
/// </summary>
public readonly record struct Mat2D(double A, double B, double C, double D, double E, double F)
{
    public static Mat2D Identity => new(1, 0, 0, 1, 0, 0);

    public static Mat2D Translate(double tx, double ty) => new(1, 0, 0, 1, tx, ty);
    public static Mat2D Scale(double sx, double sy) => new(sx, 0, 0, sy, 0, 0);
    public static Mat2D Rotate(double radians)
    {
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        return new Mat2D(cos, sin, -sin, cos, 0, 0);
    }

    public Vec2 Apply(Vec2 p) => new(
        (A * p.X) + (C * p.Y) + E,
        (B * p.X) + (D * p.Y) + F
    );

    /// <summary>Composition: this ∘ other (apply other, then this)</summary>
    public Mat2D Multiply(Mat2D other) => new(
        A * other.A + C * other.B,
        B * other.A + D * other.B,
        A * other.C + C * other.D,
        B * other.C + D * other.D,
        A * other.E + C * other.F + E,
        B * other.E + D * other.F + F
    );
}

// --- Path segments --------------------------------------------------------

public interface IPathSeg
{
    /// <summary>Enumerate points that define the segment for bounds estimation.</summary>
    IEnumerable<Vec2> ControlPoints { get; }

    /// <summary>Transform the segment by an affine matrix.</summary>
    IPathSeg Transform(Mat2D m);
}

public sealed record MoveTo(Vec2 To) : IPathSeg
{
    public IEnumerable<Vec2> ControlPoints => new[] { To };
    public IPathSeg Transform(Mat2D m) => this with { To = m.Apply(To) };
}

public sealed record LineTo(Vec2 To) : IPathSeg
{
    public IEnumerable<Vec2> ControlPoints => new[] { To };
    public IPathSeg Transform(Mat2D m) => this with { To = m.Apply(To) };
}

/// <summary>
/// Cubic Bézier: from current point to To with control points C1/C2.
/// </summary>
public sealed record CubicTo(Vec2 C1, Vec2 C2, Vec2 To) : IPathSeg
{
    public IEnumerable<Vec2> ControlPoints => new[] { C1, C2, To };
    public IPathSeg Transform(Mat2D m) => this with { C1 = m.Apply(C1), C2 = m.Apply(C2), To = m.Apply(To) };
}

public sealed record ClosePath() : IPathSeg
{
    public IEnumerable<Vec2> ControlPoints => Array.Empty<Vec2>();
    public IPathSeg Transform(Mat2D m) => this;
}

public sealed class Path2D
{
    public LayerTag Layer { get; }
    public IReadOnlyList<IPathSeg> Segments { get; }

    public Path2D(LayerTag layer, IEnumerable<IPathSeg> segments)
    {
        Layer = layer;
        Segments = segments.ToList();
        if (Segments.Count == 0 || Segments[0] is not MoveTo)
            throw new ArgumentException("Path must start with MoveTo.");
    }

    public Path2D WithLayer(LayerTag layer) => new(layer, Segments);

    public Path2D Transform(Mat2D m) => new(Layer, Segments.Select(s => s.Transform(m)));

    /// <summary>
    /// Fast bounds estimate using control points only.
    /// For cubics this is conservative-ish; for exact bounds you'd solve derivative roots.
    /// Good enough for stave sizing + debug.
    /// </summary>
    public RectD BoundsByControlPoints()
    {
        var b = RectD.Empty;
        foreach (var seg in Segments)
        {
            foreach (var p in seg.ControlPoints)
                b = b.Include(p);
        }
        return b;
    }
}

// --- Scene graph ----------------------------------------------------------

public abstract class Node
{
    public LayerTag? LayerOverride { get; init; } // optional: force children onto a layer
    public Mat2D LocalTransform { get; init; } = Mat2D.Identity;

    public abstract IEnumerable<Path2D> Flatten(Mat2D parent);
    public IEnumerable<Path2D> Flatten() => Flatten(Mat2D.Identity);

    protected LayerTag ResolveLayer(LayerTag original) => LayerOverride ?? original;
}

public sealed class Group : Node
{
    public List<Node> Children { get; } = new();

    public override IEnumerable<Path2D> Flatten(Mat2D parent)
    {
        var world = parent.Multiply(LocalTransform);
        foreach (var child in Children)
        {
            foreach (var p in child.Flatten(world))
            {
                // Apply any layer override at this group level
                yield return p.WithLayer(ResolveLayer(p.Layer));
            }
        }
    }
}

public sealed class PathNode : Node
{
    public required Path2D Path { get; init; }

    public override IEnumerable<Path2D> Flatten(Mat2D parent)
    {
        var world = parent.Multiply(LocalTransform);
        var p = Path.Transform(world);
        yield return p.WithLayer(ResolveLayer(p.Layer));
    }
}

// --- Convenience builders -------------------------------------------------

public static class Primitives
{
    /// <summary>Create a rectangle as a closed path, at (x,y) top-left in mm coordinates.</summary>
    public static Path2D Rect(LayerTag layer, double x, double y, double w, double h)
    {
        var p0 = new Vec2(x, y);
        var p1 = new Vec2(x + w, y);
        var p2 = new Vec2(x + w, y + h);
        var p3 = new Vec2(x, y + h);

        return new Path2D(layer, new IPathSeg[]
        {
            new MoveTo(p0),
            new LineTo(p1),
            new LineTo(p2),
            new LineTo(p3),
            new ClosePath()
        });
    }
}

// --- Output payload (for renderers/tests) --------------------------------

public sealed record FlatPath(LayerTag Layer, Path2D Path);

public static class Flattening
{
    public static IReadOnlyList<FlatPath> FlattenToFlatPaths(Node root)
        => root.Flatten().Select(p => new FlatPath(p.Layer, p)).ToList();

    public static RectD Bounds(IReadOnlyList<FlatPath> flat)
        => flat.Select(fp => fp.Path.BoundsByControlPoints())
               .Aggregate(RectD.Empty, (acc, b) => acc.Union(b));
}