using System.Collections.Generic;
using System.Linq;
using System.Text;
using DynamicPropertyGenerator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Sharpie;
using Sharpie.Writer;

namespace DynamicPropertyGenerator
{
    [Generator]
    public class DynamicPropertyGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            string ns = context.Compilation.AssemblyName ?? context.Compilation.ToString();
            string className = $"DynamicProperty";
            string fullName = $"{ns}.{className}";

            Class stubClass = new Class(className)
                .SetStatic(true)
                .SetNamespace(ns)
                .WithAccessibility(Accessibility.Internal)
                .WithMethod(DynamicGetMethod.Stub())
                .WithMethod(DynamicPathGetMethod.Stub())
                .WithMethod(DynamicSetStringMethod.Stub())
                .WithMethod(DynamicSetObjectMethod.Stub());

            Compilation compilation = GetStubCompilation(context, stubClass);
            INamedTypeSymbol? stubClassType = compilation.GetTypeByMetadataName(fullName);

            IEnumerable<ITypeSymbol> calls = GetStubCalls(compilation, stubClassType);

            Class generatedClass = new Class(className)
                .SetStatic(true)
                .SetNamespace(ns)
                .SetPartial(true)
                .WithAccessibility(Accessibility.Internal);

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

                    generatedClass = generatedClass.WithMethod(new DynamicGetMethod(type).Build())
                                                   .WithMethod(new DynamicPathGetMethod(type).Build())
                                                   .WithMethod(new DynamicSetObjectMethod(type).Build())
                                                   .WithMethod(new DynamicSetStringMethod(type).Build());

                    foreach (IPropertySymbol prop in type.GetAccessibleProperties())
                    {
                        types.Push(prop.Type);
                    }

                    generatedTypes.Add(type);
                }

                //foreach (ITypeSymbol type in calls)
                //{
                //    if (type is null)
                //    {
                //        continue;
                //    }

                //    if (generatedTypes.Contains(type))
                //    {
                //        continue;
                //    }

                //    var dynamicGetMethod = new DynamicGetMethod(type);
                //    var dynamicPathGetMethod = new DynamicPathGetMethod(type);
                //    var dynamicSetStringMethod = new DynamicSetStringMethod(type);
                //    var dynamicSetObjectMethod = new DynamicSetObjectMethod(type);

                //    generatedClass = generatedClass.WithMethod(dynamicGetMethod.Build())
                //                                   .WithMethod(dynamicPathGetMethod.Build())
                //                                   .WithMethod(dynamicSetStringMethod.Build())
                //                                   .WithMethod(dynamicSetObjectMethod.Build());

                //    //Class typedClass = new Class(className).SetStatic(true)
                //    //                                       .SetNamespace(ns)
                //    //                                       .SetPartial(true)
                //    //                                       .WithAccessibility(Accessibility.Internal);

                //    //var dynamicPathGetMethod = new DynamicPathGetMethod(type);
                //    //typedClass = typedClass.WithMethod(dynamicPathGetMethod.Build());

                //    //context.AddSource($"{typedClass.ClassName}.{type}", SourceText.From(ClassWriter.Write(typedClass), Encoding.UTF8));

                //    generatedTypes.Add(type);
                //}

                string str = ClassWriter.Write(generatedClass);

                context.AddSource(className, SourceText.From(str, Encoding.UTF8));
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
