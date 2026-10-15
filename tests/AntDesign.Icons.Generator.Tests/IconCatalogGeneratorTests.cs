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
            new InMemoryAdditionalText("icons/outlined/step-forward.svg", svg),
            new InMemoryAdditionalText("icons/outlined/fast-backward.svg", svg),
            new InMemoryAdditionalText("icons/outlined/column-height.svg", svg),
            new InMemoryAdditionalText("icons/outlined/account-book.svg", svg),
            new InMemoryAdditionalText("icons/filled/account-book.svg", svg),
            new InMemoryAdditionalText("icons/twotone/account-book.svg", svg),
            new InMemoryAdditionalText("icons/outlined/aim.svg", svg),
            new InMemoryAdditionalText("icons/filled/ant-design.svg", svg),
            new InMemoryAdditionalText("icons/filled/sample.svg", svg),
            new InMemoryAdditionalText("icons/outlined/appstore.svg", svg),
            new InMemoryAdditionalText("icons/outlined/appstore-add.svg", svg),
            new InMemoryAdditionalText("icons/twotone/down-circle.svg", "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1024 1024\"><path fill=\"#E6E6E6\" d=\"M0 0h1v1H0z\" /><path fill=\"#1890ff\" d=\"M0 0h1v1H0z\" /></svg>"),
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
        Assert.Contains("new(\"column-height\", \"outlined\", \"editor\", typeof(ColumnHeightOutlined))", catalog.SourceText.ToString());
        Assert.Contains("new(\"ant-design\", \"filled\", \"logo\", typeof(AntDesignFilled))", catalog.SourceText.ToString());
        Assert.Contains("new(\"sample\", \"filled\", \"other\", typeof(SampleFilled))", catalog.SourceText.ToString());
        Assert.Contains("new(\"down-circle\", \"twotone\", \"direction\", typeof(DownCircleTwoTone))", catalog.SourceText.ToString());
        Assert.Contains("\"direction\", \"suggestion\", \"editor\", \"data\", \"logo\", \"other\"", catalog.SourceText.ToString());
        var catalogText = catalog.SourceText.ToString();
        Assert.True(catalogText.IndexOf("StepBackwardOutlined", StringComparison.Ordinal) < catalogText.IndexOf("StepForwardOutlined", StringComparison.Ordinal));
        Assert.True(catalogText.IndexOf("StepForwardOutlined", StringComparison.Ordinal) < catalogText.IndexOf("FastBackwardOutlined", StringComparison.Ordinal));
        Assert.True(catalogText.IndexOf("AccountBookOutlined", StringComparison.Ordinal) < catalogText.IndexOf("AccountBookFilled", StringComparison.Ordinal));
        Assert.True(catalogText.IndexOf("AccountBookFilled", StringComparison.Ordinal) < catalogText.IndexOf("AccountBookTwoTone", StringComparison.Ordinal));
        Assert.True(catalogText.IndexOf("AccountBookTwoTone", StringComparison.Ordinal) < catalogText.IndexOf("AimOutlined", StringComparison.Ordinal));
        Assert.True(catalogText.IndexOf("AppstoreAddOutlined", StringComparison.Ordinal) < catalogText.IndexOf("AppstoreOutlined", StringComparison.Ordinal));

        var icon = Assert.Single(result.Results.SelectMany(generator => generator.GeneratedSources),
            source => source.HintName.Contains("StepBackwardOutlined.g.cs", StringComparison.Ordinal));
        var iconText = icon.SourceText.ToString();
        Assert.Contains("builder.AddAttribute(1, \"role\", role);", iconText);
        Assert.Contains("builder.AddAttribute(2, \"aria-label\", ariaLabel ?? IconName);", iconText);
        Assert.Contains("if (!string.IsNullOrWhiteSpace(iconStyle))", iconText);
        Assert.DoesNotContain("builder.AddAttribute(6, \"xmlns\"", iconText);
        Assert.DoesNotContain("xmlns=", iconText);

        var twotoneIcon = Assert.Single(result.Results.SelectMany(generator => generator.GeneratedSources),
            source => source.HintName.Contains("DownCircleTwoTone.g.cs", StringComparison.Ordinal));
        Assert.Contains("if (twoToneColor is { Length: 1 })", twotoneIcon.SourceText.ToString());
        Assert.DoesNotContain("|| string.IsNullOrWhiteSpace(twoToneColor[1])", twotoneIcon.SourceText.ToString());
    }

    private sealed class InMemoryAdditionalText(string path, string text) : AdditionalText
    {
        public override string Path { get; } = path;

        public override SourceText GetText(System.Threading.CancellationToken cancellationToken = default)
            => SourceText.From(text);
    }
}
