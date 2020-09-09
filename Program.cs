using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace includecs
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            try
            {
                string csfile;
                if (args.Length < 1 || !File.Exists(csfile = args[0]))
                {
                    await Console.Out.WriteLineAsync($"Usage: includecs [csfile]");
                    return 1;
                }

                var csFileInfo = new FileInfo(csfile);
                var csProjFile = GetCsprojFile(csFileInfo);
                
                await Console.Out.WriteLineAsync($"csprojfile = {csProjFile}");
                var csProjLines = (await File.ReadAllLinesAsync(csProjFile.FullName)).ToList();
                var compileIncludeMatch = new Regex(@"<Compile\s+Include");
                var lastCompileIncludeIndex = csProjLines.FindLastIndex(l => compileIncludeMatch.IsMatch(l));
                if(lastCompileIncludeIndex == -1)
                {
                    throw new InvalidOperationException("No compile includes found. (Right type of .csproj file?)");
                }
                var match = compileIncludeMatch.Match(csProjLines[lastCompileIncludeIndex]);
                var spacing = csProjLines[lastCompileIncludeIndex].Substring(0, match.Index);
                var relativePathOfNewFile = Path.GetRelativePath(csProjFile.Directory.FullName, csfile);
                var newCompileInclude = $"{spacing}<Compile Include=\"{relativePathOfNewFile}\" />";
                csProjLines.Insert(lastCompileIncludeIndex + 1, newCompileInclude);
                
                await File.WriteAllLinesAsync(csProjFile.FullName, csProjLines);

                return 0;
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync(ex.ToString());
                return 2;         
            }
        }

        static FileInfo GetCsprojFile(FileInfo csFileInfo)
        {
            for(var dir = csFileInfo.Directory; dir != null; dir = dir.Parent)
            {
                var csProjFiles = dir.GetFiles("*.csproj");
                if(csProjFiles.Length > 1)
                {
                    var multipleFilesList = string.Join("\n", csProjFiles.Select(fi => fi.FullName));
                    throw new InvalidOperationException($"Multiple csproj files found: \n{multipleFilesList}");
                }
                else if(csProjFiles.Length == 1)
                {
                    return csProjFiles.Single();
                }
            }
            throw new InvalidOperationException("Could not find a csproj file.");
        }
    }
}
