using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace AntDesign.Icons.Generator
{
    public class IconGeneratorHelper
    {
        public static string IconTemplate = """
            using Microsoft.AspNetCore.Components;
            using Microsoft.AspNetCore.Components.Rendering;
            
            namespace AntDesign.Icons;
            
            public partial class {{className}} : IconConponentBase
            {
                /*towtoneParameter*/

                private static string IconName = "{{iconName}}";
                private static string Path = {{svgPaths}};

            /*towtoneFields*/

                public static RenderFragment RenderIcon(string className = "", string style = "", bool spin = false, double rotate = 0/*twotoneParam*/)
                {
                    /*towtoneHandler*/

                    return (builder) =>
                    {
                        builder.OpenElement(0, "span");
                        builder.AddAttribute(1, "class", $"anticon anticon-{IconName} {(spin ? "anticon-spin" : "")} {className}");
                        builder.AddAttribute(2, "style", $"{(rotate != 0 ? $"transform: rotate({rotate}deg);" : "")} {style}");

                        builder.OpenElement(3, "svg");
                       
            {{attributes}}

                        builder.AddMarkupContent(888, Path);
                        builder.CloseElement();
                        builder.CloseElement();
                    };
                }

                protected override void BuildRenderTree(RenderTreeBuilder builder)
                    => builder.AddContent(0, RenderIcon(Class, Style, Spin, Rotate/*twotoneArgs*/));

            /*twotoneMethodTemplate*/
            }
            """;

        private static string TwoToneMethodTemplate = """
                private static string GetTwoToneIconSvg(string path, string[] twoToneColor)
                {
                    if (twoToneColor is { Length: 1 } || string.IsNullOrWhiteSpace(twoToneColor[1]))
                    {
                        return path.Replace($"fill=\"primaryColor\"", $"fill=\"{twoToneColor[0]}\"");
                    }
                    else if (twoToneColor is { Length: 2 })
                    {
                        return path.Replace($"fill=\"primaryColor\"", $"fill=\"{twoToneColor[0]}\"")
                            .Replace($"fill=\"secondaryColor\"", $"fill=\"{twoToneColor[1]}\"");
                    }
                    else
                    {
                        return path.Replace($"fill=\"primaryColor\"", $"fill=\"{DefaultPrimaryColor}\"")
                            .Replace($"fill=\"secondaryColor\"", $"fill=\"{DefaultSecondaryColor}\"");
                    }
                }
            """;

        public static string GetIconClassTemplate(string iconName, string className, string content)
        {
            var template = IconTemplate;
            if (className.EndsWith("Twotone"))
            {
                var (primaryColor, secondaryColor) = GetTwotoneFields(content);
                var fields = $"""
                        private static string DefaultPrimaryColor = "{primaryColor}";
                        private static string DefaultSecondaryColor = "{secondaryColor}";
                    """;

                content = content.Replace($"fill=\"{primaryColor}\"", "fill=\"primaryColor\"")
                    .Replace($"fill=\"{secondaryColor}\"", "fill=\"secondaryColor\"");

                var document = XDocument.Load(new StringReader(content));

                foreach (var path in document.Root.Nodes().OfType<XElement>())
                {
                    if (path.Attribute("fill")?.Value == "secondaryColor" && !string.IsNullOrWhiteSpace(secondaryColor))
                    {
                        path.SetAttributeValue("fill", "secondaryColor");
                    }
                    else if (!string.IsNullOrWhiteSpace(primaryColor))
                    {
                        path.SetAttributeValue("fill", "primaryColor");
                    }
                }

                template = template
                    .Replace("/*twotoneParam*/", ", string[] twoToneColor = null")
                    .Replace("/*towtoneFields*/", fields)
                    .Replace("/*twotoneArgs*/", ", TwoToneColor")
                    .Replace("/*towtoneParameter*/", "[Parameter] public string[] TwoToneColor { get; set; }")
                    .Replace("/*twotoneMethodTemplate*/", TwoToneMethodTemplate)
                    .Replace("/*towtoneHandler*/", "Path = GetTwoToneIconSvg(Path, twoToneColor);");

                content = document.ToString();
            }


            var xml = new XmlDocument();
            xml.LoadXml(content);

            var attributes = xml.DocumentElement.Attributes.Cast<XmlAttribute>().ToDictionary(static attr => attr.Name, static attr => attr.Value);
            var svgPaths = xml.DocumentElement.InnerXml;

            template = template.Replace("{{iconName}}", iconName)
            .Replace("{{className}}", className)
            .Replace("{{svgPaths}}", $"\"\"\"{svgPaths}\"\"\"")
            .Replace("{{attributes}}", GetAttributesString(attributes));


            return template;
        }

        private static string GetAttributesString(Dictionary<string, string> attributes)
        {
            var sb = new StringBuilder();
            var index = 5;
            foreach (var attr in attributes)
            {
                sb.AppendLine($"{Indent(3)}builder.AddAttribute({index++}, \"{attr.Key}\", \"{attr.Value}\");");
            }

            return sb.ToString();
        }

        private static (string primaryColor, string secondaryColor) GetTwotoneFields(string svgPaths)
        {
            var primaryColor = svgPaths.Contains("fill=\"#333\"") ? "#333":"";
            var secondaryColor = svgPaths switch
            {
                _ when svgPaths.Contains("fill=\"#E6E6E6\"") => "#E6E6E6",
                _ when svgPaths.Contains("fill=\"#D9D9D9\"") => "#D9D9D9",
                _ when svgPaths.Contains("fill=\"#D8D8D8\"") => "#D8D8D8",
                _ => ""
            };

            return (primaryColor, secondaryColor);

            //return $"""
            //    {Indent(1)}private static string DefaultPrimaryColor = "{primaryColor}";
            //    {Indent(1)}private static string DefaultSecondaryColor = "{secondaryColor}";
            //    """;
        }

        private static string Indent(int level)
        {
            return new string(' ', level * 4);
        }
    }
}
