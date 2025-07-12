using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourceGenerator;

[Generator]
public class WarmUpGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput(
			c => c.AddSource("WarmUp.g.cs", SourceGenerationHelper.GenerateBaseClass())
		);

		var componentProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
			"MoonTools.ECS.Component",
			predicate: (node, cancelToken) =>
			{
				return node is StructDeclarationSyntax || node is RecordDeclarationSyntax;
			},
			transform: (ctx, cancelToken) =>
			{
				var target = (BaseTypeDeclarationSyntax) ctx.TargetNode;
				return (ctx.TargetSymbol.ContainingNamespace.Name, target);
			}
		);

		var relationProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
			"MoonTools.ECS.Relation",
			predicate: (node, cancelToken) =>
			{
				return node is StructDeclarationSyntax || node is RecordDeclarationSyntax;
			},
			transform: (ctx, cancelToken) =>
			{
				var target = (BaseTypeDeclarationSyntax) ctx.TargetNode;
				return (ctx.TargetSymbol.ContainingNamespace.Name, target);
			}
		);

		context.RegisterImplementationSourceOutput(componentProvider.Collect(), (sourceProductionContext, invocationExpression) => ExecuteComponents(invocationExpression, sourceProductionContext));
		context.RegisterImplementationSourceOutput(relationProvider.Collect(), (sourceProductionContext, invocationExpression) => ExecuteRelations(invocationExpression, sourceProductionContext));
	}

	public void ExecuteComponents(System.Collections.Immutable.ImmutableArray<(string, BaseTypeDeclarationSyntax)> expressions, SourceProductionContext context)
	{
		var thign2 = expressions.Select(e => e.Item2.Identifier.ToString());
		context.AddSource("WarmUpComponentStorage.g.cs", SourceGenerationHelper.Generate(expressions.Select(e => e.Item1).Distinct(), thign2, "Component"));
	}

	public void ExecuteRelations(System.Collections.Immutable.ImmutableArray<(string, BaseTypeDeclarationSyntax)> expressions, SourceProductionContext context)
	{
		var typeNames = expressions.Select(e => e.Item2.Identifier.ToString());
		context.AddSource("WarmUpRelationStorage.g.cs", SourceGenerationHelper.Generate(expressions.Select(e => e.Item1).Distinct(), typeNames, "Relation"));
	}
}
