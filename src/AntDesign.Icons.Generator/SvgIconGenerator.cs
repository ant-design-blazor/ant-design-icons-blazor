using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace AntDesign.Icons.Generator
{
    [Generator]
    public class SvgIconGenerator : IIncrementalGenerator
    {
        private static readonly Dictionary<string, string> IconCategories = new(StringComparer.Ordinal)
        {
            ["step-backward|step-forward|fast-backward|fast-forward|shrink|arrows-alt|down|up|left|right|caret-up|caret-down|caret-left|caret-right|up-circle|down-circle|left-circle|right-circle|double-right|double-left|vertical-left|vertical-right|vertical-align-top|vertical-align-middle|vertical-align-bottom|forward|backward|rollback|enter|retweet|swap|swap-left|swap-right|arrow-up|arrow-down|arrow-left|arrow-right|play-circle|up-square|down-square|left-square|right-square|login|logout|menu-fold|menu-unfold|border-bottom|border-horizontal|border-inner|border-outer|border-left|border-right|border-top|border-verticle|pic-center|pic-left|pic-right|radius-bottomleft|radius-bottomright|radius-upleft|radius-upright|fullscreen|fullscreen-exit"] = "direction",
            ["question|question-circle|plus|plus-circle|pause|pause-circle|minus|minus-circle|plus-square|minus-square|info|info-circle|exclamation|exclamation-circle|close|close-circle|close-square|check|check-circle|check-square|clock-circle|warning|issues-close|stop"] = "suggestion",
            ["edit|form|copy|scissor|delete|snippets|diff|highlight|align-center|align-left|align-right|bg-colors|bold|italic|underline|strikethrough|redo|undo|zoom-in|zoom-out|font-colors|font-size|line-height|colum-height|colum-width|dash|small-dash|sort-ascending|sort-descending|drag|ordered-list|unordered-list|radius-setting|column-width"] = "editor",
            ["area-chart|pie-chart|bar-chart|dot-chart|line-chart|radar-chart|heat-map|fall|rise|stock|box-plot|fund|sliders"] = "data",
            ["android|apple|windows|ie|chrome|github|aliwangwang|dingding|weibo-square|weibo-circle|taobao-circle|html5|weibo|twitter|wechat|youtube|alipay-circle|taobao|skype|qq|medium-workmark|gitlab|medium|linkedin|google-plus|dropbox|facebook|codepen|code-sandbox|code-sandbox-circle|amazon|google|codepen-circle|alipay|ant-design|ant-cloud|aliyun|zhihu|slack|slack-square|behance|behance-square|dribbble|dribbble-square|instagram|yuque|alibaba|yahoo|reddit|sketch"] = "logo"
        };

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var textProvider = context.AdditionalTextsProvider.Where(static file => file.Path.EndsWith(".svg"));

            var valueProvider = textProvider.Select((text, token) =>
            {
                var fileName = Path.GetFileNameWithoutExtension(text.Path);
                var dirName = Path.GetFileName(Path.GetDirectoryName(text.Path));
               
                return new SvgFileInfo(
                    fileName,
                    text.GetText(token)!.ToString(),
                    text.Path,
                    dirName
                );
            }).Collect();

            var oldIconNamesProvider = context.AdditionalTextsProvider
                .Where(static file => file.Path.EndsWith("adjustViewBox.ts"))
                .Select((text, token) => text.GetText(token)!.ToString())
                .Collect();

            var assemblies = context.CompilationProvider.Select((compilation, token) => compilation.Assembly);

            var mergeInfo = assemblies.Combine(valueProvider.Combine(oldIconNamesProvider));

            context.RegisterSourceOutput(mergeInfo, (ctx, mergeInfo) =>
            {
                var packageName = mergeInfo.Left.Name;
                var fileInfos = mergeInfo.Right.Left;
                var oldIconNames = IconGeneratorHelper.ParseOldIconNames(mergeInfo.Right.Right.FirstOrDefault() ?? string.Empty);
                GerateIconKind(packageName, fileInfos, oldIconNames, ctx);
                GenerateCatalog(fileInfos, ctx);
            });
        }

        private void GerateIconKind(string packageName, IEnumerable<SvgFileInfo> svgFileInfos, ISet<string> oldIconNames, SourceProductionContext context)
        {
            foreach (var fileInfo in svgFileInfos)
            {
                var iconName = string.Join("", fileInfo.Name.Split('-').Select(static str => str[0].ToString().ToUpper() + str.Substring(1)));
                var iconTheme = fileInfo.DirName[0].ToString().ToUpper() + fileInfo.DirName.Substring(1);
                var className = iconName + iconTheme;
   
                var iconCode = IconGeneratorHelper.GetIconClassTemplate(fileInfo.Name, className, fileInfo.Content, oldIconNames);
                context.AddSource($"{fileInfo.DirName}/{className}.g.cs", SourceText.From(iconCode, Encoding.UTF8));
            }
        }

        private static void GenerateCatalog(IEnumerable<SvgFileInfo> svgFileInfos, SourceProductionContext context)
        {
            var entries = svgFileInfos
                .Select(file => (file.Name, Theme: file.DirName, ClassName: ToClassName(file.Name) + ToClassName(file.DirName)))
                .OrderBy(file => file.Name, StringComparer.Ordinal)
                .ThenBy(file => file.Theme, StringComparer.Ordinal)
                .ToArray();

            var source = new StringBuilder("""
                // <auto-generated />
                using System;
                using System.Collections.Generic;
                using System.Linq;

                namespace AntDesign.Icons;

                public sealed class IconCatalogItem
                {
                    public IconCatalogItem(string name, string theme, string category, Type componentType)
                    {
                        Name = name;
                        Theme = theme;
                        Category = category;
                        ComponentType = componentType;
                    }

                    public string Name { get; }
                    public string Theme { get; }
                    public string Category { get; }
                    public Type ComponentType { get; }
                }

                public static class IconCatalog
                {
                    public static IReadOnlyList<string> Categories { get; } = Array.AsReadOnly(new[] { "direction", "suggestion", "editor", "data", "logo", "other" });

                    public static IReadOnlyList<IconCatalogItem> Items { get; } = Array.AsReadOnly(new IconCatalogItem[]
                    {
                """);
            foreach (var entry in entries)
            {
                var category = GetCategory(entry.Name);
                source.Append("        new(\"").Append(entry.Name).Append("\", \"").Append(entry.Theme)
                    .Append("\", \"").Append(category).Append("\", typeof(").Append(entry.ClassName).AppendLine(")),");
            }
            source.AppendLine("""
                    });

                    public static IReadOnlyList<IconCatalogItem> ForTheme(string theme)
                        => Items.Where(item => string.Equals(item.Theme, theme, StringComparison.OrdinalIgnoreCase)).ToArray();
                }
                """);

            context.AddSource("IconCatalog.g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
        }

        private static string ToClassName(string value)
            => string.Concat(value.Split('-').Select(part => char.ToUpperInvariant(part[0]) + part.Substring(1)));

        // Keep the documentation taxonomy alongside the generated inventory so every
        // newly added SVG receives a category without a second hand-maintained list.
        private static string GetCategory(string name)
            => IconCategories.FirstOrDefault(category => category.Key.Split('|').Contains(name)).Value ?? "other";


    }
}
