using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Stafr.Models;

public class StyleModel
{
    public string Name { get; set; } = "";
    public int Version { get; set; } = 0;
    public Fonts? Fonts { get; set; }
    public Layout? Layout { get; set; }
    public Border? Border { get; set; }
}

public class Fonts
{
    public FontSpec? Heading { get; set; }
    public FontSpec? Body { get; set; }
}

public class FontSpec
{
    public string? File { get; set; }
    public double SizeMm { get; set; }
}

public class Layout
{
    public double PaddingMm { get; set; }
    public double IconBoxMm { get; set; }
    public MetaBlockSpec? MetaBlock { get; set; }
}

public class MetaBlockSpec
{
    public double LineSpacingMm { get; set; }
    public int MaxLines { get; set; }
}

//TODO: I probably won't have a short-side border, so this is overkill. But I'll keep it for now.
public class Border
{
    public LongSideBorder? LongSide { get; set; }
}

public class LongSideBorder
{
    public string? LeftCap { get; set; }
    public string? Tile { get; set; }
    public string? RightCap { get; set; }
    public double TileAdvanceMm { get; set; }
    public double OverlapMm { get; set; }
    public bool MirrorEveryOther { get; set; }
}

public static class StyleLoader
{
    public static StyleModel LoadFromFile(string styleYamlPath)
    {
        var yaml = File.ReadAllText(styleYamlPath);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var style = deserializer.Deserialize<StyleModel>(yaml);
        Validate(style, styleYamlPath);
        return style;
    }

    private static void Validate(StyleModel style, string path)
    {
        if (string.IsNullOrWhiteSpace(style.Name))
            throw new InvalidDataException($"Style missing name: {path}");
        if (style.Version <= 0)
            throw new InvalidDataException($"Style version must be > 0: {path}");

        //NOTE: These ! are ugly here, but works for now...
        if (style.Border!.LongSide!.TileAdvanceMm <= 0)
            throw new InvalidDataException($"TileAdvanceMm must be > 0: {path}");
        if (style.Layout!.PaddingMm < 0)
            throw new InvalidDataException($"PaddingMm must be >= 0: {path}");
    }
}