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
                .WithMethod(StubGetMethod())
                .WithMethod(StubSetMethod());

            Compilation compilation = GetStubCompilation(context, stubClass);
            INamedTypeSymbol stubClassType = compilation.GetTypeByMetadataName(fullName);

            IEnumerable<ITypeSymbol> calls = GetStubCalls(compilation, stubClassType);

            Class generatedClass = new Class(className)
                .SetStatic(true)
                .SetNamespace(ns)
                .WithAccessibility(Accessibility.Internal);

            var generatedTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

            if (calls.Any())
            {
                foreach (ITypeSymbol type in calls)
                {
                    if (type is null)
                    {
                        continue;
                    }

                    if (generatedTypes.Contains(type))
                    {
                        continue;
                    }

                    generatedClass = generatedClass.WithMethod(BuildGetMethod(type))
                                                   .WithMethod(BuildSetMethod(type));

                    generatedTypes.Add(type);
                }

                string str = ClassWriter.Write(generatedClass);

                context.AddSource(className, SourceText.From(str, Encoding.UTF8));
            }
            else
            {
                context.AddSource(stubClass.ClassName, SourceText.From(ClassWriter.Write(stubClass), Encoding.UTF8));
            }
        }

        private static Method BuildGetMethod(ITypeSymbol type)
        {
            IEnumerable<IPropertySymbol> properties = type.GetAccessibleProperties();

            Argument[] getArguments = GetMethodArguments(type.ToString());

            string noPropertyException = $"throw new System.ArgumentOutOfRangeException(nameof({getArguments[1].Name}), $\"Type '{type}' has no property of name '{{{getArguments[1].Name}}}'\")";

            void ifBody(BodyWriter ifBodyWriter)
            {
                var caseExpressions = new List<CaseExpression>();

                foreach (IPropertySymbol prop in properties)
                {
                    var caseExpression = new CaseExpression($"\"{prop.Name.ToLower()}\"", $"{getArguments[0].Name}.{prop.Name}");
                    caseExpressions.Add(caseExpression);
                }

                ifBodyWriter.WriteReturnSwitchExpression(new SwitchCaseExpression($"{getArguments[1].Name}.ToLower()", caseExpressions, noPropertyException));
            }

            void elseBody(BodyWriter elseBodyWriter)
            {
                var caseStatements = new List<CaseExpression>();

                foreach (IPropertySymbol prop in properties)
                {
                    var caseStatement = new CaseExpression($"\"{prop.Name}\"", $"{getArguments[0].Name}.{prop.Name}");
                    caseStatements.Add(caseStatement);
                }

                elseBodyWriter.WriteReturnSwitchExpression(new SwitchCaseExpression(getArguments[1].Name, caseStatements, noPropertyException));
            }

            var getMethod = new Method(Accessibility.Public, true, false, "object", "Get", getArguments, (getBodyWriter) =>
            {
                var ifCasing = new IfStatement(new If(getArguments[2].Name, ifBody), elseBody);

                getBodyWriter.WriteIf(ifCasing);
            });

            return getMethod;
        }

        private static Method BuildSetMethod(ITypeSymbol type)
        {
            IEnumerable<IPropertySymbol> properties = type.GetAccessibleProperties();

            Argument[] setArguments = SetMethodArguments(type.ToString());

            void ifBody(BodyWriter ifBodyWriter)
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

                ifBodyWriter.WriteSwitchCaseStatement(
                    new SwitchCaseStatement(
                        $"{setArguments[1].Name}.ToLower()",
                        caseStatements,
                        $"throw new System.ArgumentOutOfRangeException(nameof({setArguments[1].Name}), $\"Type '{type}' has no property of name '{{{setArguments[1].Name}}}'\");"));
            }

            void elseBody(BodyWriter elseBodyWriter)
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
                    setArguments[1].Name, caseStatements,
                    $"throw new System.ArgumentOutOfRangeException(nameof({setArguments[1].Name}), $\"Type '{type}' has no property of name '{{{setArguments[1].Name}}}'\");"));
            }

            var setMethod = new Method(Accessibility.Public, true, false, "void", "Set", setArguments, (setBodyWriter) =>
            {
                var ifCasing = new IfStatement(new If(setArguments[3].Name, ifBody), elseBody);

                setBodyWriter.WriteIf(ifCasing);
            });

            return setMethod;
        }

        private static Method StubGetMethod() => new Method(Accessibility.Public, true, false, "object", "Get", GetMethodArguments("object"), ReturnNewObjectWriter);

        private static Method StubSetMethod() => new Method(Accessibility.Public, true, false, "object", "Set", SetMethodArguments("object"), string.Empty);

        private static Argument[] GetMethodArguments(string type) => new Argument[]
            {
                new(type, "obj"),
                new("string", "name"),
                new("bool", "ignoreCasing", "false"),
            };

        private static Argument[] SetMethodArguments(string type) => new Argument[]
            {
                new(type, "obj"),
                new("string", "name"),
                new("string", "value"),
                new("bool", "ignoreCasing", "false"),
            };

        private static void ReturnNewObjectWriter(BodyWriter bodyWriter) => bodyWriter.WriteReturn("new object()");

        private static Compilation GetStubCompilation(GeneratorExecutionContext context, Class stubClass)
        {
            Compilation compilation = context.Compilation;

            var options = (compilation as CSharpCompilation)?.SyntaxTrees[0].Options as CSharpParseOptions;

            return compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(ClassWriter.Write(stubClass), Encoding.UTF8), options));
        }

        private static IEnumerable<ITypeSymbol> GetStubCalls(Compilation compilation, INamedTypeSymbol stubClassType)
        {
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
                            ITypeSymbol argumentType = semanticModel.GetTypeInfo(argument).Type;
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
