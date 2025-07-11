using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace SourceGenerator;

public static class SourceGenerationHelper
{
    public static string Generate(IEnumerable<string> assemblyNames, IEnumerable<string> nameDeclarations)
    {
        StringBuilder builder = new StringBuilder();
		foreach (var name in assemblyNames)
		{
			builder.AppendLine($"using {name};");
		}

        builder.AppendLine("public static class Warmup {");
        builder.AppendLine("public static void WarmUpStorages(MoonTools.ECS.World world) {");

        foreach (var storageType in nameDeclarations)
        {
            builder.AppendLine($"world.CreateStorage<{storageType}>();");
        }


        builder.AppendLine("}");
        builder.AppendLine("}");
        return builder.ToString();
    }
}
