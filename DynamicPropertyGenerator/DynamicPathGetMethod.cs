using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using DynamicPropertyGenerator.Extensions;
using Microsoft.CodeAnalysis;
using Sharpie;
using Sharpie.Writer;

namespace DynamicPropertyGenerator
{
    internal class DynamicPathGetMethod
    {
        private const string MethodName = "Get";
        private const string ReturnType = "object";

        private readonly ITypeSymbol _type;
        private readonly ImmutableArray<Argument> _arguments;
        private readonly Lazy<ImmutableArray<IPropertySymbol>> _properties;
        private readonly string _noPropertyException;

        public DynamicPathGetMethod(ITypeSymbol type)
        {
            _type = type;
            _arguments = Arguments(type.ToString()).ToImmutableArray();
            _noPropertyException = $"throw new System.ArgumentException($\"No property '{{name}}' found in type '{_type}'\", nameof({_arguments[1].Name}))";

            _properties = new Lazy<ImmutableArray<IPropertySymbol>>(() => _type.GetAccessibleProperties().ToImmutableArray());
        }

        private static Argument[] Arguments(string type) => new Argument[]
            {
                new(type, "obj", true),
                new("System.Collections.Generic.Stack<string>", "path"),
                new("bool", "ignoreCasing", "false"),
            };

        private void IgnoreCase(BodyWriter ifBodyWriter)
        {
            var caseExpressions = new List<CaseExpression>();

            foreach (IPropertySymbol prop in _properties.Value)
            {
                var caseExpression = new CaseExpression($"\"{prop.Name.ToLower()}\"", $"{_arguments[1].Name}.Count == 0 ? {_arguments[0].Name}.{prop.Name} : {MethodName}({_arguments[0].Name}.{prop.Name}, {_arguments[1].Name}, {_arguments[2].Name})");
                caseExpressions.Add(caseExpression);
            }

            ifBodyWriter.WriteReturnSwitchExpression(new SwitchCaseExpression($"name.ToLower()", caseExpressions, _noPropertyException));
        }

        private void CaseSensitive(BodyWriter elseBodyWriter)
        {
            var caseExpressions = new List<CaseExpression>();

            foreach (IPropertySymbol prop in _properties.Value)
            {
                var caseExpression = new CaseExpression($"\"{prop.Name}\"", $"{_arguments[1].Name}.Count == 0 ? {_arguments[0].Name}.{prop.Name} : {MethodName}({_arguments[0].Name}.{prop.Name}, {_arguments[1].Name}, {_arguments[2].Name})");
                caseExpressions.Add(caseExpression);
            }

            elseBodyWriter.WriteReturnSwitchExpression(new SwitchCaseExpression("name", caseExpressions, _noPropertyException));
        }

        public Method Build()
        {
            var ifStmt = new IfStatement(new If(_arguments[2].Name, IgnoreCase), CaseSensitive);

            return GetMethod(_arguments, (getBodyWriter) =>
            {
                getBodyWriter.WriteVariable("name", $"{_arguments[1].Name}.Pop()");
                getBodyWriter.WriteIf(ifStmt);
            });
        }

        public static Method Stub() => GetMethod(Arguments("object"), (BodyWriter bodyWriter) => bodyWriter.WriteReturn("new object()"));

        private static Method GetMethod(IEnumerable<Argument> arguments, Action<BodyWriter> body) => new(Accessibility.Public, true, false, ReturnType, MethodName, arguments, body);
    }
}
