using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace SourceGenerator;

public static class SourceGenerationHelper
{
	public static string GenerateBaseClass()
	{
		StringBuilder builder = new StringBuilder();
		builder.AppendLine("public static partial class Warmup {");
		builder.AppendLine("public static void WarmUpStorages(MoonTools.ECS.World world) {");
		builder.AppendLine("WarmUpComponentStorages(world);");
		builder.AppendLine("WarmUpRelationStorages(world);");
		builder.AppendLine("}");
		builder.AppendLine("}");
		return builder.ToString();
	}

    public static string Generate(IEnumerable<string> namespaceNames, IEnumerable<string> nameDeclarations, string attributeName)
	{
		StringBuilder builder = new StringBuilder();
		foreach (var name in namespaceNames)
		{
			builder.AppendLine($"using {name};");
		}

		builder.AppendLine("public static partial class Warmup {");
		builder.AppendLine($"private static void WarmUp{attributeName}Storages(MoonTools.ECS.World world) {{");

		foreach (var storageType in nameDeclarations)
		{
			builder.AppendLine($"world.WarmUp{attributeName}<{storageType}>();");
		}


		builder.AppendLine("}");
		builder.AppendLine("}");
		return builder.ToString();
	}
}
