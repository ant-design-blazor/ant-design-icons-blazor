using AntDesign.Icons.Generator;
using Xunit;

namespace AntDesign.Icons.Generator.Tests;

public class IconGeneratorHelperTests
{
    [Theory]
    [InlineData("icon")]
    [InlineData("source-icon custom-class")]
    public void Generated_svg_does_not_copy_source_root_class(string sourceClass)
    {
        var svg = $"<svg xmlns=\"http://www.w3.org/2000/svg\" class=\"{sourceClass}\" viewBox=\"0 0 1024 1024\"><path d=\"M0 0h1v1H0z\" /></svg>";

        var generated = IconGeneratorHelper.GetIconClassTemplate("sample", "SampleOutlined", svg, new HashSet<string>());

        Assert.DoesNotContain("builder.AddAttribute(4, \"class\"", generated);
        Assert.Contains("\"viewBox\", \"64 64 896 896\"", generated);
        Assert.Contains("\"data-icon\", \"sample\"", generated);
    }

    [Fact]
    public void Generated_svg_uses_the_upstream_old_icon_names_for_legacy_view_boxes()
    {
        const string transformSource = "const OLD_ICON_NAMES = ['step-backward', 'loading'];";
        var oldIconNames = IconGeneratorHelper.ParseOldIconNames(transformSource);
        const string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1024 1024\"><path d=\"M0 0h1v1H0z\" /></svg>";

        var oldGenerated = IconGeneratorHelper.GetIconClassTemplate("step-backward", "StepBackwardOutlined", svg, oldIconNames);
        var modernGenerated = IconGeneratorHelper.GetIconClassTemplate("down", "DownOutlined", svg, oldIconNames);

        Assert.Contains("\"viewBox\", \"0 0 1024 1024\"", oldGenerated);
        Assert.Contains("\"viewBox\", \"64 64 896 896\"", modernGenerated);
    }
}
