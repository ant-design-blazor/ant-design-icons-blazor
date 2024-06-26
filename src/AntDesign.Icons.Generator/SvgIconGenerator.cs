using Microsoft.CodeAnalysis;
using System.Text.RegularExpressions;

namespace AntDesign.Icons.Generator
{
    [Generator]
    public class SvgIconGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var textProvider = context.AdditionalTextsProvider.Where(static file => file.Path.EndsWith(".svg"));

            var valueProvider = textProvider.Select((text, token) =>
            {
                var fileName = Path.GetFileNameWithoutExtension(text.Path);
                var iconName = string.Join("", fileName.Split('-').Select(static str => str[0].ToString().ToUpper() + str.Substring(1)));

                var dirName = Path.GetFileName(Path.GetDirectoryName(text.Path));
                var theme = dirName[0].ToString().ToUpper() + dirName.Substring(1);

                return new SvgFileInfo(
                    iconName,
                    text.GetText(token)!.ToString(),
                    text.Path,
                    theme
                );
            }).Collect();

            var assemblies = context.CompilationProvider.Select((compilation, token) => compilation.Assembly);

            var mergeInfo = assemblies.Combine(valueProvider);

            context.RegisterSourceOutput(mergeInfo, (ctx, mergeInfo) =>
            {
                var packageName = mergeInfo.Left.Name;
                var fileInfos = mergeInfo.Right;
                GerateIconKind(packageName, fileInfos, ctx);
            });
        }

        private void GerateIconKind(string packageName, IEnumerable<SvgFileInfo> svgFileInfos, SourceProductionContext context)
        {
            //context.AddSource()
        }
    }

    internal class SvgFileInfo
    {
        public string Name { get; }
        public string Content { get; }
        public string Path { get; }
        public string Theme { get; }

        public SvgFileInfo(string name, string content, string path, string theme)
        {
            Name = name;
            Content = content;
            Path = path;
            Theme = theme;
        }
    }
}
