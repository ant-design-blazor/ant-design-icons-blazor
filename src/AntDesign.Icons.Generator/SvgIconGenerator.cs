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
            ["StepBackward|StepForward|FastBackward|FastForward|Shrink|ArrowsAlt|Down|Up|Left|Right|CaretUp|CaretDown|CaretLeft|CaretRight|UpCircle|DownCircle|LeftCircle|RightCircle|DoubleRight|DoubleLeft|VerticalLeft|VerticalRight|VerticalAlignTop|VerticalAlignMiddle|VerticalAlignBottom|Forward|Backward|Rollback|Enter|Retweet|Swap|SwapLeft|SwapRight|ArrowUp|ArrowDown|ArrowLeft|ArrowRight|PlayCircle|UpSquare|DownSquare|LeftSquare|RightSquare|Login|Logout|MenuFold|MenuUnfold|BorderBottom|BorderHorizontal|BorderInner|BorderOuter|BorderLeft|BorderRight|BorderTop|BorderVerticle|PicCenter|PicLeft|PicRight|RadiusBottomleft|RadiusBottomright|RadiusUpleft|RadiusUpright|Fullscreen|FullscreenExit"] = "direction",
            ["Question|QuestionCircle|Plus|PlusCircle|Pause|PauseCircle|Minus|MinusCircle|PlusSquare|MinusSquare|Info|InfoCircle|Exclamation|ExclamationCircle|Close|CloseCircle|CloseSquare|Check|CheckCircle|CheckSquare|ClockCircle|Warning|IssuesClose|Stop"] = "suggestion",
            ["Edit|Form|Copy|Scissor|Delete|Snippets|Diff|Highlight|AlignCenter|AlignLeft|AlignRight|BgColors|Bold|Italic|Underline|Strikethrough|Redo|Undo|ZoomIn|ZoomOut|FontColors|FontSize|LineHeight|Dash|SmallDash|SortAscending|SortDescending|Drag|OrderedList|UnorderedList|RadiusSetting|ColumnWidth|ColumnHeight"] = "editor",
            ["AreaChart|PieChart|BarChart|DotChart|LineChart|RadarChart|HeatMap|Fall|Rise|Stock|BoxPlot|Fund|Sliders"] = "data",
            ["Android|Apple|Windows|Ie|IeCircle|IeSquare|Chrome|Github|Aliwangwang|Dingding|WeiboSquare|WeiboCircle|TaobaoCircle|Html5|Weibo|Twitter|TwitterCircle|TwitterSquare|Wechat|WhatsApp|Youtube|AlipayCircle|Taobao|TaobaoSquare|Dingtalk|DingtalkCircle|DingtalkSquare|Skype|Qq|QqCircle|QqSquare|MediumWorkmark|Gitlab|Medium|MediumCircle|MediumSquare|Linkedin|GooglePlus|GooglePlusCircle|GooglePlusSquare|Dropbox|DropboxCircle|DropboxSquare|Facebook|Codepen|CodepenSquare|CodeSandbox|CodeSandboxSquare|CodeSandboxCircle|Amazon|AmazonCircle|AmazonSquare|Google|GoogleCircle|GoogleSquare|CodepenCircle|Alipay|AlipaySquare|AntDesign|AntCloud|Aliyun|Zhihu|ZhihuCircle|ZhihuSquare|Slack|SlackCircle|SlackSquare|Behance|BehanceCircle|BehanceSquare|Dribbble|DribbbleCircle|DribbbleSquare|Instagram|Yuque|Alibaba|Yahoo|Reddit|RedditCircle|RedditSquare|Sketch|SketchCircle|SketchSquare|WechatWork|OpenAI|Anthropic|Claude|Gemini|Mistral|DeepSeek|Qwen|Perplexity|HuggingFace|Ollama|Replicate|ElevenLabs|Telegram|Mastodon|Threads|Snapchat|Discord|X|Bilibili|Pinterest|TikTok|Spotify|Twitch|Linux|Java|JavaScript|Python|Ruby|DotNet|Kubernetes|Docker|Baidu|HarmonyOS"] = "logo"
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
                var iconTheme = ToThemeClassName(fileInfo.DirName);
                var className = iconName + iconTheme;
   
                var iconCode = IconGeneratorHelper.GetIconClassTemplate(fileInfo.Name, className, fileInfo.Content, oldIconNames);
                context.AddSource($"{fileInfo.DirName}/{className}.g.cs", SourceText.From(iconCode, Encoding.UTF8));
            }
        }

        private static void GenerateCatalog(IEnumerable<SvgFileInfo> svgFileInfos, SourceProductionContext context)
        {
            var applicationIconOrder = svgFileInfos
                .GroupBy(file => file.Name, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(file => ToClassName(file.Name) + ToThemeClassName(file.DirName))
                        .OrderBy(className => className, StringComparer.Ordinal)
                        .First(),
                    StringComparer.Ordinal);
            var entries = svgFileInfos
                .Select(file => (file.Name, Theme: file.DirName, ClassName: ToClassName(file.Name) + ToThemeClassName(file.DirName)))
                .OrderBy(file => GetCategoryOrder(file.Name))
                .ThenBy(file => GetNameOrder(file.Name))
                // The React application-icon inventory follows the first generated
                // component export name for each base icon (e.g. AppstoreAdd precedes
                // Appstore), then presents that icon's variants in theme order.
                .ThenBy(file => GetNameOrder(file.Name) == int.MaxValue ? applicationIconOrder[file.Name] : string.Empty, StringComparer.Ordinal)
                .ThenBy(file => GetThemeOrder(file.Theme))
                .ThenBy(file => file.Name, StringComparer.Ordinal)
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

        private static string ToThemeClassName(string value)
            => string.Equals(value, "twotone", StringComparison.Ordinal) ? "TwoTone" : ToClassName(value);

        private static readonly string[] CategoryOrder = ["direction", "suggestion", "editor", "data", "logo", "other"];
        private static readonly string[] ThemeOrder = ["outlined", "filled", "twotone"];

        private static int GetCategoryOrder(string name)
            => Array.IndexOf(CategoryOrder, GetCategory(name));

        private static int GetThemeOrder(string theme)
            => Array.IndexOf(ThemeOrder, theme);

        private static int GetNameOrder(string name)
        {
            var componentName = ToClassName(name);
            foreach (var category in IconCategories.Keys)
            {
                var names = category.Split('|');
                var index = Array.IndexOf(names, componentName);
                if (index >= 0) return index;
            }

            return int.MaxValue;
        }

        // Keep the documentation taxonomy alongside the generated inventory so every
        // newly added SVG receives a category without a second hand-maintained list.
        private static string GetCategory(string name)
        {
            var componentName = ToClassName(name);
            return IconCategories.FirstOrDefault(category => category.Key.Split('|').Contains(componentName)).Value ?? "other";
        }


    }
}
