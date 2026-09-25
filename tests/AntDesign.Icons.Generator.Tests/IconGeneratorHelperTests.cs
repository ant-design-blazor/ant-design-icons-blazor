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

        var generated = IconGeneratorHelper.GetIconClassTemplate("sample", "SampleOutlined", svg);

        Assert.DoesNotContain("builder.AddAttribute(4, \"class\"", generated);
        Assert.Contains("\"viewBox\", \"0 0 1024 1024\"", generated);
        Assert.Contains("\"data-icon\", \"sample\"", generated);
    }
}
