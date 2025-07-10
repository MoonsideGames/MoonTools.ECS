using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace SourceGenerator;

public static class SourceGenerationHelper
{
    public static string Generate(IEnumerable<GenericNameSyntax> nameDeclarations)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("public static Warmup {");
        builder.AppendLine("public static void WarmUpStorages(World world) {");

        foreach (var storageType in nameDeclarations)
        {
            builder.AppendLine($"world.CreateStorage<{storageType.Identifier.ToString()}>();");
        }


        builder.AppendLine("}");
        builder.AppendLine("}");
        return builder.ToString();
    }
}
