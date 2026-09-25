using AntDesign.Icons.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace AntDesign.Icons.Generator.Tests;

public class IconCatalogGeneratorTests
{
    [Fact]
    public void Generator_emits_icon_name_theme_category_and_component_type_from_svg_inputs()
    {
        const string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1024 1024\"><path d=\"M0 0h1v1H0z\" /></svg>";
        var additionalFiles = new AdditionalText[]
        {
            new InMemoryAdditionalText("icons/outlined/step-backward.svg", svg),
            new InMemoryAdditionalText("icons/filled/sample.svg", svg),
            new InMemoryAdditionalText("icons/transforms/adjustViewBox.ts", "const OLD_ICON_NAMES = ['step-backward'];")
        };
        var compilation = CSharpCompilation.Create("AntDesign.Icons");
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: new[] { new SvgIconGenerator().AsSourceGenerator() },
            additionalTexts: additionalFiles);

        var result = driver.RunGenerators(compilation).GetRunResult();
        var catalog = Assert.Single(result.Results.SelectMany(generator => generator.GeneratedSources),
            source => source.HintName == "IconCatalog.g.cs");

        Assert.Contains("new(\"step-backward\", \"outlined\", \"direction\", typeof(StepBackwardOutlined))", catalog.SourceText.ToString());
        Assert.Contains("new(\"sample\", \"filled\", \"other\", typeof(SampleFilled))", catalog.SourceText.ToString());
        Assert.Contains("\"direction\", \"suggestion\", \"editor\", \"data\", \"logo\", \"other\"", catalog.SourceText.ToString());
    }

    private sealed class InMemoryAdditionalText(string path, string text) : AdditionalText
    {
        public override string Path { get; } = path;

        public override SourceText GetText(System.Threading.CancellationToken cancellationToken = default)
            => SourceText.From(text);
    }
}
