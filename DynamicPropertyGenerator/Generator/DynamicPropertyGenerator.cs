using System.Collections.Generic;
using System.Linq;
using System.Text;
using DynamicPropertyGenerator.Extensions;
using DynamicPropertyGenerator.Methods;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Sharpie;
using Sharpie.Writer;

namespace DynamicPropertyGenerator.Generator
{
    [Generator]
    public class DynamicPropertyGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            string ns = context.Compilation.AssemblyName ?? context.Compilation.ToString();
            const string className = "DynamicProperty";
            string fullName = $"{ns}.{className}";

            var methods = new IDynamicMethodBuilder[]
            {
                new DynamicGetMethod(),
                new DynamicPathGetMethod(),
                new DynamicSetObjectMethod(),
                new DynamicSetStringMethod(),
            };

            Class stubClass = new Class(className)
                .SetStatic(true)
                .SetNamespace(ns)
                .WithAccessibility(Accessibility.Internal);

            foreach (IDynamicMethodBuilder method in methods)
            {
                stubClass = stubClass.WithMethod(method.Stub());
            }

            Compilation compilation = GetStubCompilation(context, stubClass);
            INamedTypeSymbol? stubClassType = compilation.GetTypeByMetadataName(fullName);

            IEnumerable<ITypeSymbol> calls = GetStubCalls(compilation, stubClassType);

            var generatedTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

            if (calls.Any())
            {
                var types = new Stack<ITypeSymbol>(calls);
                while (types.Count > 0)
                {
                    ITypeSymbol? type = types.Pop();

                    if (type is null)
                    {
                        continue;
                    }

                    if (generatedTypes.Contains(type))
                    {
                        continue;
                    }

                    Class generatedClass = new Class(className).SetStatic(true)
                                                               .SetNamespace(ns)
                                                               .SetPartial(true)
                                                               .WithAccessibility(Accessibility.Internal);

                    foreach (IDynamicMethodBuilder method in methods)
                    {
                        generatedClass = generatedClass.WithMethod(method.Build(type));
                    }

                    foreach (IPropertySymbol prop in type.GetAccessibleProperties())
                    {
                        types.Push(prop.Type);
                    }

                    generatedTypes.Add(type);

                    string str = ClassWriter.Write(generatedClass);

                    context.AddSource($"{className}.{type}", SourceText.From(str, Encoding.UTF8));
                }
            }
            else
            {
                context.AddSource(stubClass.ClassName, SourceText.From(ClassWriter.Write(stubClass), Encoding.UTF8));
            }
        }

        private static Compilation GetStubCompilation(GeneratorExecutionContext context, Class stubClass)
        {
            Compilation compilation = context.Compilation;

            var options = (compilation as CSharpCompilation)?.SyntaxTrees[0].Options as CSharpParseOptions;

            return compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(ClassWriter.Write(stubClass), Encoding.UTF8), options));
        }

        private static IEnumerable<ITypeSymbol> GetStubCalls(Compilation compilation, INamedTypeSymbol? stubClassType)
        {
            if (stubClassType is null)
            {
                yield break;
            }

            foreach (SyntaxTree tree in compilation.SyntaxTrees)
            {
                SemanticModel semanticModel = compilation.GetSemanticModel(tree);
                foreach (InvocationExpressionSyntax invocation in tree.GetRoot().DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
                {
                    if (semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol symbol && symbol.ContainingType is { })
                    {
                        if (SymbolEqualityComparer.Default.Equals(symbol.ContainingType, stubClassType))
                        {
                            ExpressionSyntax argument = invocation.ArgumentList.Arguments.First().Expression;
                            ITypeSymbol? argumentType = semanticModel.GetTypeInfo(argument).Type;
                            if (argumentType is null)
                            {
                                continue;
                            }
                            yield return argumentType;
                        }
                    }
                }
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }
}
