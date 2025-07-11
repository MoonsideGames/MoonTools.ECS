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

		var assemblyNames = context.MetadataReferencesProvider.Select(m => m.WithProperties())
		IncrementalValuesProvider<GenericNameSyntax> invocationProvider = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: (node, cancelToken) =>
            {
                // find all invocations of MoonTools.ECS.World.Set method
                if (node is InvocationExpressionSyntax invocationExpression)
                {
					return invocationExpression.DescendantNodes(n => n == node).OfType<GenericNameSyntax>().Any(g => g.Identifier.ToString() == "Get");
                }

                return false;
            },
            transform: (ctx, cancelToken) =>
            {
                //the transform is called only when the predicate returns true
                var invocationExpression = (InvocationExpressionSyntax)ctx.Node;
				return invocationExpression.DescendantNodes(n => n == invocationExpression).OfType<GenericNameSyntax>().First(g => g.Identifier.ToString() == "Get");
            }
        );

		var collected = assemblyNames.Combine(invocationProvider.Collect()).Collect();
        context.RegisterImplementationSourceOutput(collected, (sourceProductionContext, invocationExpression) => Execute(invocationExpression, sourceProductionContext));
    }

    public void Execute(System.Collections.Immutable.ImmutableArray<(GenericNameSyntax, string)> expressions, SourceProductionContext context)
    {
		var thing = expressions.Select(e => e.Item1.DescendantNodes().OfType<TypeArgumentListSyntax>().First()).Select(t => t.DescendantNodes().OfType<IdentifierNameSyntax>().First()).Select(i => i.ToString()).Distinct();
        context.AddSource("WarmUpECSStorage.g.cs", SourceGenerationHelper.Generate(expressions.Select(e => e.Item2).Distinct(), thing));
    }
}
