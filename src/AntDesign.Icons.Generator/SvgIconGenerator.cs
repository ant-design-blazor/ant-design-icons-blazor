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
            foreach (var fileInfo in svgFileInfos)
            {
                var iconName = string.Join("", fileInfo.Name.Split('-').Select(static str => str[0].ToString().ToUpper() + str.Substring(1)));
                var iconTheme = fileInfo.DirName[0].ToString().ToUpper() + fileInfo.DirName.Substring(1);
                var className = iconName + iconTheme;
   
                var iconCode = IconGeneratorHelper.GetIconClassTemplate(fileInfo.Name, className, fileInfo.Content);
                context.AddSource($"{fileInfo.DirName}/{className}.g.cs", SourceText.From(iconCode, Encoding.UTF8));
            }
        }


    }
}
