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
        // Add the marker attribute to the compilation
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "WarmUpECSStorage.g.cs", 
            SourceText.From(SourceGenerationHelper.Generate([]), Encoding.UTF8)));

        IncrementalValuesProvider<InvocationExpressionSyntax> invocationProvider = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: (node, cancelToken) =>
            {
                // find all invocations of MoonTools.ECS.World.Set method
                if (node is InvocationExpressionSyntax invocationExpression)
                {
                    var methodDeclaration = node.Ancestors().OfType<MethodDeclarationSyntax>().First();
                    if (methodDeclaration.Identifier.ToString() == "Set")
                    {
                        var classDeclaration = node.Ancestors().OfType<ClassDeclarationSyntax>().First();
                        if (classDeclaration.Identifier.ToString() == "World")
                        {
                            var namespaceDeclaration = node.Ancestors().OfType<NamespaceDeclarationSyntax>().First();
                            if (namespaceDeclaration.Name.ToString() == "MoonTools.ECS")
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            },
            transform: (ctx, cancelToken) =>
            {
                //the transform is called only when the predicate returns true
                //so for example if we have one class named Calculator
                //this will only be called once regardless of how many other classes exist
                var invocationExpression = (InvocationExpressionSyntax)ctx.Node;
                return invocationExpression;
            }
        );

        IncrementalValuesProvider<ClassDeclarationSyntax> classProvider = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: (node, cancelToken) =>
            {
                if (node is ClassDeclarationSyntax classDeclaration)
                {
                    var namespaceDeclaration = node.Ancestors().OfType<NamespaceDeclarationSyntax>().First();
                    if (namespaceDeclaration.Name.ToString() == "MoonTools.ECS")
                    {
                        if (classDeclaration.Identifier.ToString() == "Warmup")
                        {
                            return true;
                        }
                    }
                }

                return false;
            },
            transform: (ctx, cancelToken) =>
            {
                return (ClassDeclarationSyntax) ctx.Node;
            }
        );

        var collected = invocationProvider.Collect();
        context.RegisterImplementationSourceOutput(collected, (sourceProductionContext, invocationExpression) => Execute(invocationExpression, sourceProductionContext));
    }

    public void Execute(System.Collections.Immutable.ImmutableArray<InvocationExpressionSyntax> expressions, SourceProductionContext context)
    {
        var thing = expressions.Select(e => e.DescendantNodes().OfType<GenericNameSyntax>().First());
        context.AddSource("WarmUpECSStorage.g.cs", SourceGenerationHelper.Generate(thing));
    }
}
