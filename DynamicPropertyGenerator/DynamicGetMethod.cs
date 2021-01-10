using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using DynamicPropertyGenerator.Extensions;
using Microsoft.CodeAnalysis;
using Sharpie;
using Sharpie.Writer;

namespace DynamicPropertyGenerator
{
    internal class DynamicGetMethod
    {
        private const string MethodName = "Get";
        private const string ReturnType = "object";

        private readonly ITypeSymbol _type;
        private readonly ImmutableArray<Argument> _arguments;
        private readonly Lazy<ImmutableArray<IPropertySymbol>> _properties;
        private readonly string _noPropertyException;

        public DynamicGetMethod(ITypeSymbol type)
        {
            _type = type;
            _arguments = Arguments(type.ToString()).ToImmutableArray();
            _noPropertyException = $"throw new System.ArgumentOutOfRangeException(nameof({_arguments[1].Name}), $\"Type '{_type}' has no property of name '{{{_arguments[1].Name}}}'\")";

            _properties = new Lazy<ImmutableArray<IPropertySymbol>>(() => _type.GetAccessibleProperties().ToImmutableArray());
        }

        private static Argument[] Arguments(string type) => new Argument[]
            {
                new(type, "obj"),
                new("string", "name"),
                new("bool", "ignoreCasing", "false"),
            };


        private void IfBody(BodyWriter ifBodyWriter)
        {
            var caseExpressions = new List<CaseExpression>();

            foreach (IPropertySymbol prop in _properties.Value)
            {
                var caseExpression = new CaseExpression($"\"{prop.Name.ToLower()}\"", $"{_arguments[0].Name}.{prop.Name}");
                caseExpressions.Add(caseExpression);
            }

            ifBodyWriter.WriteReturnSwitchExpression(new SwitchCaseExpression($"{_arguments[1].Name}.ToLower()", caseExpressions, _noPropertyException));
        }

        private void ElseBody(BodyWriter elseBodyWriter)
        {
            var caseStatements = new List<CaseExpression>();

            foreach (IPropertySymbol prop in _properties.Value)
            {
                var caseStatement = new CaseExpression($"\"{prop.Name}\"", $"{_arguments[0].Name}.{prop.Name}");
                caseStatements.Add(caseStatement);
            }

            elseBodyWriter.WriteReturnSwitchExpression(new SwitchCaseExpression(_arguments[1].Name, caseStatements, _noPropertyException));
        }

        public Method Build()
        {
            var ifCasing = new IfStatement(new If(_arguments[2].Name, IfBody), ElseBody);

            return new Method(Accessibility.Public, true, false, ReturnType, MethodName, _arguments, (getBodyWriter) => getBodyWriter.WriteIf(ifCasing));
        }

        public static Method Stub() => new Method(Accessibility.Public, true, false, "object", "Get", Arguments("object"), (BodyWriter bodyWriter) => bodyWriter.WriteReturn("new object()"));
    }
}
