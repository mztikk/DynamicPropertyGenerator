﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DynamicPropertyGenerator.Extensions;
using Microsoft.CodeAnalysis;
using Sharpie;
using Sharpie.Writer;

namespace DynamicPropertyGenerator.Methods.Set
{
    internal class DynamicPathSetStringMethod : IDynamicMethodBuilder
    {
        private const string MethodName = "Set";
        private const string ReturnType = "void";

        private ITypeSymbol? _type;
        private ImmutableArray<Parameter> _arguments;
        private ImmutableArray<IPropertySymbol> _properties;
        private string? _noPropertyException;

        private static Parameter[] Arguments(string type) => new Parameter[]
            {
                new(type, "obj", true),
                new("System.Collections.Generic.Queue<string>", "path"),
                new("string", "value"),
                new("bool", "ignoreCasing", "false"),
            };

        private Action<BodyWriter> GetSetValueWriter(IPropertySymbol prop)
        {
            string fullTypeName = prop.Type.ToString().TrimEnd('?');

            void SetValue(BodyWriter bodyWriter)
            {
                string value;
                if (prop.Type.Name == "String")
                {
                    value = _arguments[2].Name;
                }
                else
                {
                    value = $"{fullTypeName}.Parse({_arguments[2].Name})";
                }

                var ifPathEnd = new IfStatement(
                    new If($"{_arguments[1].Name}.Count == 0",
                    (ifBody) => ifBody.WriteAssignment($"{_arguments[0].Name}.{prop.Name}", value)),
                    (elseBody) => elseBody.WriteLine($"{MethodName}({_arguments[0].Name}.{prop.Name}, {_arguments[1].Name}, {_arguments[2].Name}, {_arguments[3].Name});"));

                bodyWriter.WriteIf(ifPathEnd).WriteBreak();
            }

            return SetValue;
        }

        private void IgnoreCase(BodyWriter ifBodyWriter)
        {
            var caseStatements = new List<CaseStatement>();
            foreach (IPropertySymbol prop in _properties.Where(prop => prop.Type.HasStringParse() || prop.Type.Name == "String"))
            {
                string fullTypeName = prop.Type.ToString().TrimEnd('?');

                var caseStatement = new CaseStatement($"\"{prop.Name.ToLower()}\"", GetSetValueWriter(prop));
                caseStatements.Add(caseStatement);
            }

            ifBodyWriter.WriteSwitchCaseStatement(new SwitchCaseStatement("name.ToLower()", caseStatements, _noPropertyException));
        }

        private void CaseSensitive(BodyWriter elseBodyWriter)
        {
            var caseStatements = new List<CaseStatement>();
            foreach (IPropertySymbol prop in _properties.Where(prop => prop.Type.HasStringParse() || prop.Type.Name == "String"))
            {
                string fullTypeName = prop.Type.ToString().TrimEnd('?');

                var caseStatement = new CaseStatement($"\"{prop.Name}\"", GetSetValueWriter(prop));
                caseStatements.Add(caseStatement);
            }

            elseBodyWriter.WriteSwitchCaseStatement(new SwitchCaseStatement("name", caseStatements, _noPropertyException));
        }

        public Method Build(ITypeSymbol type)
        {
            _type = type;
            _arguments = Arguments(type.ToString()).ToImmutableArray();
            _noPropertyException = $"throw new System.ArgumentException($\"No property '{{name}}' found in type '{_type}'\", nameof({_arguments[1].Name}));";

            _properties = _type.GetAccessibleProperties().ToImmutableArray();

            var ifStmt = new IfStatement(new If(_arguments[3].Name, IgnoreCase), CaseSensitive);

            return GetMethod(_arguments, (setBodyWriter) =>
            {
                setBodyWriter.WriteVariable("name", $"{_arguments[1].Name}.Dequeue()");
                setBodyWriter.WriteIf(ifStmt);
            });
        }

        public Method Stub() => GetMethod(Arguments("object"), string.Empty);

        private static Method GetMethod(IEnumerable<Parameter> arguments, Action<BodyWriter> body) => new(Accessibility.Public, true, false, ReturnType, MethodName, arguments, body);
        private static Method GetMethod(IEnumerable<Parameter> arguments, string body) => new(Accessibility.Public, true, false, ReturnType, MethodName, arguments, body);
    }
}
