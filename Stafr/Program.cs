using Stafr.Models;
using Stafr.Services;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

if (args.Length != 3)
{
    Console.WriteLine("Usage: stafr <input.yaml> <template.svg> <output.svg>");
    return;
}

var inputPath = Path.GetFullPath(args[0]);
var templatePath = Path.GetFullPath(args[1]);
var outputPath = Path.GetFullPath(args[2]);

if (!File.Exists(inputPath))
{
    Console.WriteLine($"Input YAML file does not exist: {inputPath}");
    return;
}

if (!File.Exists(templatePath))
{
    Console.WriteLine($"Template SVG file does not exist: {templatePath}");
    return;
}

var outputDirectory = Path.GetDirectoryName(outputPath) ?? Directory.GetCurrentDirectory();
if (!Directory.Exists(outputDirectory))
{
    Console.WriteLine($"Output directory does not exist: {outputDirectory}");
    return;
}

var yaml = File.ReadAllText(inputPath);

var deserializer = new DeserializerBuilder()
    .WithNamingConvention(CamelCaseNamingConvention.Instance)
    .Build();

var metadata = deserializer.Deserialize<PatternMetadata>(yaml);

var svgTemplate = File.ReadAllText(templatePath);

var renderer = new SvgTemplateRenderer();
var result = renderer.Render(svgTemplate, metadata);

File.WriteAllText(outputPath, result);

Console.WriteLine($"Generated: {outputPath}");
