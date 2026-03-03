using System.Collections.Concurrent;
using Stafr.Geometry;
using Stafr.Services;

namespace Stafr.Models;

public class StyleAssets
{
    private readonly string _styleRoot;
    private readonly ConcurrentDictionary<string, Path2D> _cache = new();

    public StyleAssets(string styleRoot)
    {
        _styleRoot = styleRoot;
    }

    public Path2D LoadPathAsset(string relativePath, LayerTag layer)
    {
        // Cache key should include layer, since Path2D carries it
        var key = $"{relativePath}::{layer}";
        return _cache.GetOrAdd(key, _ =>
        {
            var fullPath = Path.Combine(_styleRoot, relativePath);
            var svgText = File.ReadAllText(fullPath);
            var path = SvgPathImporter.ImportFirstPath(svgText, layer);

            // Optional: normalize / simplify / ensure closed for fill usage
            // e.g. verify it ends with ClosePath() if you expect closed motifs
            return path;
        });
    }
}