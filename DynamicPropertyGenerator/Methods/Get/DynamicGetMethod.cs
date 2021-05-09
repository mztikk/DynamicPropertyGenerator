using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using DynamicPropertyGenerator.Extensions;
using Microsoft.CodeAnalysis;
using Sharpie;
using Sharpie.Writer;

namespace DynamicPropertyGenerator.Methods.Get
{
    internal class DynamicGetMethod : IDynamicMethodBuilder
    {
        private const string MethodName = "Get";
        private const string ReturnType = "object";

        private ITypeSymbol? _type;
        private ImmutableArray<Parameter> _arguments;
        private ImmutableArray<IPropertySymbol> _properties;
        private string? _noPropertyException;

        private static Parameter[] Arguments(string type) => new Parameter[]
            {
                new(type, "obj", true),
                new("string", "name"),
                new("bool", "ignoreCasing", "false"),
            };

        private void IgnoreCase(BodyWriter ifBodyWriter)
        {
            var caseExpressions = new List<CaseExpression>();

            foreach (IPropertySymbol prop in _properties)
            {
                var caseExpression = new CaseExpression($"\"{prop.Name.ToLower()}\"", $"{_arguments[0].Name}.{prop.Name}");
                caseExpressions.Add(caseExpression);
            }

            ifBodyWriter.WriteReturnSwitchExpression(new SwitchCaseExpression($"{_arguments[1].Name}.ToLower()", caseExpressions, _noPropertyException));
        }

        private void CaseSensitive(BodyWriter elseBodyWriter)
        {
            var caseStatements = new List<CaseExpression>();

            foreach (IPropertySymbol prop in _properties)
            {
                var caseStatement = new CaseExpression($"\"{prop.Name}\"", $"{_arguments[0].Name}.{prop.Name}");
                caseStatements.Add(caseStatement);
            }

            elseBodyWriter.WriteReturnSwitchExpression(new SwitchCaseExpression(_arguments[1].Name, caseStatements, _noPropertyException));
        }

        public Method Build(ITypeSymbol type)
        {
            _type = type;
            _arguments = Arguments(type.ToString()).ToImmutableArray();
            _noPropertyException = $"throw new System.ArgumentException($\"No property '{{{_arguments[1].Name}}}' found in type '{_type}'\", nameof({_arguments[1].Name}))";

            _properties = _type.GetAccessibleProperties().ToImmutableArray();

            var ifStmt = new IfStatement(new If(_arguments[2].Name, IgnoreCase), CaseSensitive);

            return GetMethod(_arguments, (getBodyWriter) => getBodyWriter.WriteIf(ifStmt));
        }

        public Method Stub() => GetMethod(Arguments("object"), (bodyWriter) => bodyWriter.WriteReturn("new object()"));

        private static Method GetMethod(IEnumerable<Parameter> arguments, Action<BodyWriter> body) => new(Accessibility.Public, true, false, ReturnType, MethodName, arguments, body);
    }
}
