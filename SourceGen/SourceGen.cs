using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace SourceGenerator;

[Generator]
public class WarmUpGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
		System.Diagnostics.Debugger.Launch();

		var attributeProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
			"MoonTools.ECS.Component",
			predicate: (node, cancelToken) =>
			{
				return node is RecordDeclarationSyntax;
			},
			transform: (ctx, cancelToken) =>
			{
				var target = (BaseTypeDeclarationSyntax) ctx.TargetNode;
				return (ctx.TargetSymbol.ContainingNamespace.Name, target);
			}
		);

        context.RegisterImplementationSourceOutput(attributeProvider.Collect(), (sourceProductionContext, invocationExpression) => Execute(invocationExpression, sourceProductionContext));
    }

    public void Execute(System.Collections.Immutable.ImmutableArray<(string, BaseTypeDeclarationSyntax)> expressions, SourceProductionContext context)
    {
		var thign2 = expressions.Select(e => e.Item2.Identifier.ToString());
        context.AddSource("WarmUpECSStorage.g.cs", SourceGenerationHelper.Generate(expressions.Select(e => e.Item1).Distinct(), thign2));
    }

	private static string? ExtractName(NameSyntax? name)
	{
		return name switch
		{
			SimpleNameSyntax ins => ins.Identifier.Text,
			QualifiedNameSyntax qns => qns.Right.Identifier.Text,
			_ => null
		};
	}
}
