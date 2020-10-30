using System.Collections.Generic;
using System.Linq;
using System.Text;
using DynamicPropertyGenerator.Extensions;
using Microsoft.CodeAnalysis;
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
            Class c = new Class("DynamicProperty")
                .SetNamespace(context.Compilation.AssemblyName)
                .SetStatic(true)
                .WithAccessibility(Accessibility.Public);

            //Debugger.Launch();

            foreach (INamedTypeSymbol type in GetAllPublicTypesWithProperties(context.Compilation))
            {
                IEnumerable<IPropertySymbol> properties = type.GetAccessibleProperties();

                var getArguments = new List<Argument> {
                    new(type.ToString(), "obj"),
                    new("string", "name"),
                };

                var getMethod = new Method(Accessibility.Public, true, false, "object", "Get", getArguments, (getBodyWriter) =>
                {
                    var caseStatements = new List<CaseStatement>();

                    foreach (IPropertySymbol prop in properties)
                    {
                        var caseStatement = new CaseStatement($"\"{prop.Name}\"", (caseWriter) =>
                        {
                            caseWriter.WriteReturn($"{getArguments[0].Name}.{prop.Name}");
                        });
                        caseStatements.Add(caseStatement);
                    }

                    getBodyWriter.WriteSwitchCaseStatement(new(
                        getArguments[1].Name,
                        caseStatements,
                        $"throw new System.ArgumentOutOfRangeException(nameof({getArguments[1].Name}), $\"Type '{type}' has no property of name '{{{getArguments[1].Name}}}'\");"));
                });

                c.WithMethod(getMethod);

                var setArguments = new List<Argument> {
                    new(type.ToString(), "obj"),
                    new("string", "name"),
                    new("string", "value"),
                };

                var setMethod = new Method(Accessibility.Public, true, false, "void", "Set", setArguments, (setBodyWriter) =>
                {
                    var caseStatements = new List<CaseStatement>();
                    foreach (IPropertySymbol prop in properties.Where(prop => prop.Type.HasStringParse() || prop.Type.Name == "String"))
                    {
                        string fullTypeName = prop.Type.ToString().TrimEnd('?');

                        var caseStatement = new CaseStatement($"\"{prop.Name}\"", (caseWriter) =>
                        {
                            //caseWriter.WriteReturn($"{setArguments[0].Name}.{prop.Name}");
                            string value;
                            if (prop.Type.Name == "String")
                            {
                                value = setArguments[2].Name;
                            }
                            else
                            {
                                value = $"{fullTypeName}.Parse({setArguments[2].Name})";
                            }

                            caseWriter.WriteAssignment($"{setArguments[0].Name}.{prop.Name}", value);
                            caseWriter.WriteBreak();
                        });
                        caseStatements.Add(caseStatement);

                    }
                    setBodyWriter.WriteSwitchCaseStatement(new(
                        setArguments[1].Name,
                        caseStatements,
                        $"throw new System.ArgumentOutOfRangeException(nameof({setArguments[1].Name}), $\"Type '{type}' has no property of name '{{{setArguments[1].Name}}}'\");"));
                });

                c.WithMethod(setMethod);
            }

            string str = ClassWriter.Write(c);

            context.AddSource("DynamicProperty", SourceText.From(str, Encoding.UTF8));
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        private static IEnumerable<INamedTypeSymbol> GetAllPublicTypesWithProperties(Compilation compilation) => GetAllTypesWithProperties(compilation).Where(x => x.DeclaredAccessibility == Accessibility.Public && x.TypeParameters.Length == 0);
        private static IEnumerable<INamedTypeSymbol> GetAllTypesWithProperties(Compilation compilation) => GetAllTypes(compilation).Where(x => !x.IsStatic && x.GetAccessibleProperties().Any());

        private static IEnumerable<INamedTypeSymbol> GetAllTypes(Compilation compilation)
        {
            foreach (INamedTypeSymbol symbol in GetAllPublicTypes(compilation.Assembly.GlobalNamespace))
            {
                yield return symbol;
            }

            foreach (MetadataReference item in compilation.References)
            {
                if (compilation.GetAssemblyOrModuleSymbol(item) is IAssemblySymbol assemblySymbol)
                {
                    foreach (INamedTypeSymbol symbol in GetAllPublicTypes(assemblySymbol.GlobalNamespace))
                    {
                        yield return symbol;
                    }
                }
            }
        }

        private static IEnumerable<INamedTypeSymbol> GetAllPublicTypes(params INamespaceOrTypeSymbol[] symbols)
        {
            var stack = new Stack<INamespaceOrTypeSymbol>(symbols);

            while (stack.Count > 0)
            {
                INamespaceOrTypeSymbol item = stack.Pop();

                if (item is INamedTypeSymbol type && type.DeclaredAccessibility == Accessibility.Public)
                {
                    yield return type;
                }

                foreach (ISymbol member in item.GetMembers())
                {
                    if (member is INamespaceOrTypeSymbol child && child.DeclaredAccessibility == Accessibility.Public && (member is not INamedTypeSymbol typeSymbol || typeSymbol.TypeParameters.Length == 0))
                    {
                        stack.Push(child);
                    }
                }
            }
        }

        private static IEnumerable<INamedTypeSymbol> GetAllTypes(params INamespaceOrTypeSymbol[] symbols)
        {
            var stack = new Stack<INamespaceOrTypeSymbol>(symbols);

            while (stack.Count > 0)
            {
                INamespaceOrTypeSymbol item = stack.Pop();

                if (item is INamedTypeSymbol type)
                {
                    yield return type;
                }

                foreach (ISymbol member in item.GetMembers())
                {
                    if (member is INamespaceOrTypeSymbol child)
                    {
                        stack.Push(child);
                    }
                }
            }
        }
    }
}
