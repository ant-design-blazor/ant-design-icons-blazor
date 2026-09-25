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
        private static readonly DiagnosticDescriptor MissingViewBoxTransform = new(
            "ADICONS001",
            "Missing React viewBox transform source",
            "Could not read OLD_ICON_NAMES from adjustViewBox.ts; refusing to generate potentially mismatched icon viewBox values",
            "AntDesign.Icons.Generator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var allAdditionalFiles = context.AdditionalTextsProvider;
            var textProvider = allAdditionalFiles.Where(static file => file.Path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase));

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

            var viewBoxTransformProvider = allAdditionalFiles
                .Where(static file => file.Path.EndsWith("adjustViewBox.ts", StringComparison.OrdinalIgnoreCase))
                .Select((text, token) => text.GetText(token)?.ToString() ?? string.Empty)
                .Collect();

            var assemblies = context.CompilationProvider.Select((compilation, token) => compilation.Assembly);

            var mergeInfo = assemblies.Combine(valueProvider).Combine(viewBoxTransformProvider);

            context.RegisterSourceOutput(mergeInfo, (ctx, mergeInfo) =>
            {
                var packageName = mergeInfo.Left.Left.Name;
                var fileInfos = mergeInfo.Left.Right;
                var viewBoxTransformSource = mergeInfo.Right.FirstOrDefault();

                if (viewBoxTransformSource is null || !TryParseOldIconNames(viewBoxTransformSource, out var oldIconNames))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(MissingViewBoxTransform, Location.None));
                    return;
                }

                GerateIconKind(packageName, fileInfos, oldIconNames, ctx);
            });
        }

        private static bool TryParseOldIconNames(string source, out HashSet<string> oldIconNames)
        {
            var listMatch = Regex.Match(source, @"\bOLD_ICON_NAMES\s*=\s*\[(?<names>[\s\S]*?)\]", RegexOptions.CultureInvariant);
            oldIconNames = new HashSet<string>(StringComparer.Ordinal);
            if (!listMatch.Success)
            {
                return false;
            }

            foreach (Match nameMatch in Regex.Matches(listMatch.Groups["names"].Value, "['\"](?<name>[^'\"]+)['\"]", RegexOptions.CultureInvariant))
            {
                oldIconNames.Add(nameMatch.Groups["name"].Value);
            }

            return oldIconNames.Count > 0;
        }

        private void GerateIconKind(string packageName, IEnumerable<SvgFileInfo> svgFileInfos, HashSet<string> oldIconNames, SourceProductionContext context)
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


    }
}
