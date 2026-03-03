using System;
using Stafr.Geometry;

namespace Stafr.Services;

public class BorderComposer
{
    /// <summary>
    /// Builds a border along a single long side of the stave.
    /// X increases along the side. Y is the side baseline reference.
    /// </summary>
    public static Group TileLongSide(
        Path2D leftCap,
        Path2D tile,
        Path2D rightCap,
        LayerTag layer,
        double x0,
        double y0,
        double lengthMm,
        double tileAdvanceMm,
        double overlapMm,
        bool mirrorEveryOther,
        bool placeAboveLine // if true, you might translate motifs upward/downward depending on your coordinate system
    )
    {
        var g = new Group();

        // Helper: add path with translation (and optional mirror)
        void Add(Path2D p, double tx, double ty, bool mirrorX)
        {
            // Mirror around local motif's origin by scaling X = -1 and then translating.
            // If your motif isn't centered at x=0, you might want a "motifWidth" and mirror around its center.
            var m = Mat2D.Translate(tx, ty);
            if (mirrorX)
            {
                // Mirror in X and keep it in place by translating; this assumes motif anchored at x=0.
                // If you author motifs with their left edge at x=0, this works well.
                m = Mat2D.Translate(tx, ty).Multiply(Mat2D.Scale(-1, 1));
            }

            g.Children.Add(new PathNode
            {
                Path = p.WithLayer(layer),
                LocalTransform = m
            });
        }

        // 1) Place left cap at start
        Add(leftCap, x0, y0, mirrorX: false);

        // 2) Compute usable middle run
        // NOTE: If caps have widths you want to account for, store "capWidthMm" in style.yaml and offset xStart/xEnd.
        // For simplicity, we assume caps are designed to overlap tile region and/or you don't mind overlap.
        var xStart = x0;
        var xEnd = x0 + lengthMm;

        // 3) Tile across. We intentionally overlap slightly to avoid hairline gaps.
        var step = Math.Max(0.01, tileAdvanceMm - overlapMm);

        int i = 0;
        for (var x = xStart; x <= xEnd; x += step, i++)
        {
            var doMirror = mirrorEveryOther && (i % 2 == 1);
            Add(tile, x, y0, mirrorX: doMirror);
        }

        // 4) Place right cap at end
        Add(rightCap, x0 + lengthMm, y0, mirrorX: false);

        return g;
    }
}