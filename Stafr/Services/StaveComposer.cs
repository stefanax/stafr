using System.Text.RegularExpressions;
using Stafr.Geometry;
using Stafr.Models;

namespace Stafr.Services;

public class StaveComposer
{
    public Node Compose(StyleModel style, StyleAssets assets, double staveLengthMm, double staveHeightMm)
    {
        var root = new Geometry.Group();

        // Cut outline
        var outline = Primitives.Rect(LayerTag.Cut, 0, 0, staveLengthMm, staveHeightMm);
        root.Children.Add(new PathNode { Path = outline });

        // Border assets (engrave/fill)
        var ls = style?.Border?.LongSide;
        var leftCap = assets.LoadPathAsset(ls?.LeftCap!, LayerTag.Engrave);
        var tile = assets.LoadPathAsset(ls?.Tile!, LayerTag.Engrave);
        var rightCap = assets.LoadPathAsset(ls?.RightCap!, LayerTag.Engrave);

        // Place two long sides (top and bottom). Adjust y as your coordinate convention needs.
        root.Children.Add(BorderComposer.TileLongSide(leftCap, tile, rightCap, LayerTag.Engrave,
            x0: 0, y0: 0, lengthMm: staveLengthMm,
            tileAdvanceMm: (double)ls?.TileAdvanceMm!, overlapMm: (double)ls?.OverlapMm!, mirrorEveryOther: (bool)ls?.MirrorEveryOther!,
            placeAboveLine: true));

        root.Children.Add(BorderComposer.TileLongSide(leftCap, tile, rightCap, LayerTag.Engrave,
            x0: 0, y0: staveHeightMm, lengthMm: staveLengthMm,
            tileAdvanceMm: ls.TileAdvanceMm, overlapMm: ls.OverlapMm, mirrorEveryOther: ls.MirrorEveryOther,
            placeAboveLine: false));

        // TODO: icon + text + metadata, all on Engrave layer (as paths)
        // - icon: assets.LoadPathAsset(style.Icons["shirt"], LayerTag.Engrave) then translate
        // - text: font->paths later

        return root;
    }
}