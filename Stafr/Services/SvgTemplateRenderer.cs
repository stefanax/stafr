using System.Globalization;
using Stafr.Models;

namespace Stafr.Services;

public sealed class SvgTemplateRenderer
{
    public string Render(string template, PatternMetadata metadata)
    {
        var complexityDots = GenerateComplexityDots(metadata.Complexity);

        return template
            .Replace("{{NAME}}", Escape(metadata.Name))
            .Replace("{{FABRIC_METERS}}",
                metadata.Fabric.Meters.ToString(CultureInfo.InvariantCulture))
            .Replace("{{FABRIC_WIDTH}}",
                metadata.Fabric.WidthCm.ToString(CultureInfo.InvariantCulture))
            .Replace("{{COMPLEXITY}}", complexityDots);
    }

    private static string GenerateComplexityDots(int level)
    {
        level = Math.Clamp(level, 0, 3);

        return string.Concat(
            Enumerable.Range(0, 3)
                .Select(i => i < level ? "●" : "○"));
    }

    private static string Escape(string input)
    {
        return System.Security.SecurityElement.Escape(input) ?? input;
    }
}