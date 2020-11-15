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
            string ns = context.Compilation.AssemblyName ?? context.Compilation.ToString();

            //Class c = new Class("DynamicProperty")
            //    .SetNamespace(context.Compilation.AssemblyName)
            //    .SetStatic(true)
            //    .WithAccessibility(Accessibility.Public);

            //Debugger.Launch();

            foreach (INamedTypeSymbol type in GetAllPublicTypesWithProperties(context.Compilation))
            {
                string fullTypeName = type.ToString();
                string className = $"DynamicProperty_{fullTypeName.Replace('.', '_')}_Extensions";

                Class c = new Class(className)
                    .SetStatic(true)
                    .SetNamespace(ns)
                    .WithAccessibility(Accessibility.Internal);

                IEnumerable<IPropertySymbol> properties = type.GetAccessibleProperties();

                var getArguments = new List<Argument> {
                    new(type.ToString(), "obj", true),
                    new("string", "name"),
                    new("bool", "ignoreCasing", "false"),
                };

                var getMethod = new Method(Accessibility.Public, true, false, "object", "DynamicGet", getArguments, (getBodyWriter) =>
                {
                    string noPropertyException = $"throw new System.ArgumentOutOfRangeException(nameof({getArguments[1].Name}), $\"Type '{type}' has no property of name '{{{getArguments[1].Name}}}'\")";

                    var ifCasing = new IfStatement(new If[]{ new If(getArguments[2].Name,
                        (ifBodyWriter) =>
                        {
                            var caseExpressions = new List<CaseExpression>();

                            foreach (IPropertySymbol prop in properties)
                            {
                                var caseExpression = new CaseExpression($"\"{prop.Name.ToLower()}\"", $"{getArguments[0].Name}.{prop.Name}");
                                caseExpressions.Add(caseExpression);
                            }

                            ifBodyWriter.WriteReturnSwitchExpression(new SwitchCaseExpression($"{getArguments[1].Name}.ToLower()", caseExpressions, noPropertyException));

                        }) },
                        (elseBodyWriter) =>
                        {
                            var caseStatements = new List<CaseExpression>();

                            foreach (IPropertySymbol prop in properties)
                            {
                                var caseStatement = new CaseExpression($"\"{prop.Name}\"", $"{getArguments[0].Name}.{prop.Name}");
                                caseStatements.Add(caseStatement);
                            }

                            elseBodyWriter.WriteReturnSwitchExpression(new SwitchCaseExpression(getArguments[1].Name, caseStatements, noPropertyException));
                        });

                    getBodyWriter.WriteIf(ifCasing);
                });

                c.WithMethod(getMethod);

                var setArguments = new List<Argument> {
                    new(type.ToString(), "obj", true),
                    new("string", "name"),
                    new("string", "value"),
                    new("bool", "ignoreCasing", "false"),
                };

                var setMethod = new Method(Accessibility.Public, true, false, "void", "DynamicSet", setArguments, (setBodyWriter) =>
                {
                    var ifCasing = new IfStatement(new If[] { new(setArguments[3].Name,
                        (ifBodyWriter) =>
                        {
                            var caseStatements = new List<CaseStatement>();
                            foreach (IPropertySymbol prop in properties.Where(prop => prop.Type.HasStringParse() || prop.Type.Name == "String"))
                            {
                                string fullTypeName = prop.Type.ToString().TrimEnd('?');

                                var caseStatement = new CaseStatement($"\"{prop.Name.ToLower()}\"", (caseWriter) =>
                                {
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
                            ifBodyWriter.WriteSwitchCaseStatement(new SwitchCaseStatement(
                                $"{setArguments[1].Name}.ToLower()",
                                caseStatements,
                                $"throw new System.ArgumentOutOfRangeException(nameof({setArguments[1].Name}), $\"Type '{type}' has no property of name '{{{setArguments[1].Name}}}'\");"));
                        }) },
                        (elseBodyWriter) =>
                        {
                            var caseStatements = new List<CaseStatement>();
                            foreach (IPropertySymbol prop in properties.Where(prop => prop.Type.HasStringParse() || prop.Type.Name == "String"))
                            {
                                string fullTypeName = prop.Type.ToString().TrimEnd('?');

                                var caseStatement = new CaseStatement($"\"{prop.Name}\"", (caseWriter) =>
                                {
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
                            elseBodyWriter.WriteSwitchCaseStatement(new SwitchCaseStatement(
                                setArguments[1].Name,
                                caseStatements,
                                $"throw new System.ArgumentOutOfRangeException(nameof({setArguments[1].Name}), $\"Type '{type}' has no property of name '{{{setArguments[1].Name}}}'\");"));
                        });

                    setBodyWriter.WriteIf(ifCasing);
                });

                c.WithMethod(setMethod);

                string str = ClassWriter.Write(c);

                context.AddSource(className, SourceText.From(str, Encoding.UTF8));
            }
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
                    if (member is INamespaceOrTypeSymbol child
                        && child.DeclaredAccessibility == Accessibility.Public
                        && (member is not INamedTypeSymbol typeSymbol || typeSymbol.TypeParameters.Length == 0))
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
