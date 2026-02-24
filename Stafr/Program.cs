using Stafr.Models;
using Stafr.Services;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

if (args.Length != 3)
{
    Console.WriteLine("Usage: stafr <input.yaml> <template.svg> <output.svg>");
    return;
}

var inputPath = args[0];
var templatePath = args[1];
var outputPath = args[2];

var yaml = File.ReadAllText(inputPath);

var deserializer = new DeserializerBuilder()
    .WithNamingConvention(CamelCaseNamingConvention.Instance)
    .Build();

var metadata = deserializer.Deserialize<PatternMetadata>(yaml);

var svgTemplate = File.ReadAllText(templatePath);

var renderer = new SvgTemplateRenderer();
var result = renderer.Render(svgTemplate, metadata);

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
File.WriteAllText(outputPath, result);

Console.WriteLine($"Generated: {outputPath}");