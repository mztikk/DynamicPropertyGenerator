using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using DynamicPropertyGenerator.Extensions;
using Microsoft.CodeAnalysis;
using Sharpie;
using Sharpie.Writer;

namespace DynamicPropertyGenerator
{
    internal class DynamicSetObjectMethod
    {
        private const string MethodName = "Set";
        private const string ReturnType = "void";

        private readonly ITypeSymbol _type;
        private readonly ImmutableArray<Argument> _arguments;
        private readonly Lazy<ImmutableArray<IPropertySymbol>> _properties;
        private readonly string _noPropertyException;

        public DynamicSetObjectMethod(ITypeSymbol type)
        {
            _type = type;
            _arguments = Arguments(type.ToString()).ToImmutableArray();
            _noPropertyException = $"throw new System.ArgumentOutOfRangeException(nameof({_arguments[1].Name}), $\"Type '{type}' has no property of name '{{{_arguments[1].Name}}}'\");";

            _properties = new Lazy<ImmutableArray<IPropertySymbol>>(() => _type.GetAccessibleProperties().ToImmutableArray());
        }

        private static Argument[] Arguments(string type) => new Argument[]
            {
                new(type, "obj", true),
                new("string", "name"),
                new("object", "value"),
                new("bool", "ignoreCasing", "false"),
            };

        private void IfBody(BodyWriter ifBodyWriter)
        {
            var caseStatements = new List<CaseStatement>();
            foreach (IPropertySymbol prop in _properties.Value)
            {
                string fullTypeName = prop.Type.ToString().TrimEnd('?');

                var caseStatement = new CaseStatement($"\"{prop.Name.ToLower()}\"", (caseWriter) =>
                {
                    string value = $"({fullTypeName}){_arguments[2].Name}";

                    caseWriter.WriteAssignment($"{_arguments[0].Name}.{prop.Name}", value);
                    caseWriter.WriteBreak();
                });
                caseStatements.Add(caseStatement);
            }

            ifBodyWriter.WriteSwitchCaseStatement(new SwitchCaseStatement($"{_arguments[1].Name}.ToLower()", caseStatements, _noPropertyException));
        }

        private void ElseBody(BodyWriter elseBodyWriter)
        {
            var caseStatements = new List<CaseStatement>();
            foreach (IPropertySymbol prop in _properties.Value)
            {
                string fullTypeName = prop.Type.ToString().TrimEnd('?');

                var caseStatement = new CaseStatement($"\"{prop.Name}\"", (caseWriter) =>
                {
                    string value = $"({fullTypeName}){_arguments[2].Name}";

                    caseWriter.WriteAssignment($"{_arguments[0].Name}.{prop.Name}", value);
                    caseWriter.WriteBreak();
                });
                caseStatements.Add(caseStatement);

            }

            elseBodyWriter.WriteSwitchCaseStatement(new SwitchCaseStatement(_arguments[1].Name, caseStatements, _noPropertyException));
        }

        public Method Build()
        {
            var ifStmt = new IfStatement(new If(_arguments[3].Name, IfBody), ElseBody);

            return GetMethod(_arguments, (setBodyWriter) => { setBodyWriter.WriteIf(ifStmt); });
        }

        public static Method Stub() => GetMethod(Arguments("object"), string.Empty);

        private static Method GetMethod(IEnumerable<Argument> arguments, Action<BodyWriter> body) => new(Accessibility.Public, true, false, ReturnType, MethodName, arguments, body);
        private static Method GetMethod(IEnumerable<Argument> arguments, string body) => new(Accessibility.Public, true, false, ReturnType, MethodName, arguments, body);
    }
}
