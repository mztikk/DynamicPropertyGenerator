using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Sharpie;
using Sharpie.Writer;

namespace DynamicPropertyGenerator.Methods.Set
{
    internal class DynamicSetObjectMethod : IDynamicMethodBuilder
    {
        private const string MethodName = "Set";
        private const string ReturnType = "void";

        private static Parameter[] Arguments(string type) => new Parameter[]
            {
                new(type, "obj", true),
                new("string", "path"),
                new("object", "value"),
                new("bool", "ignoreCasing", "false"),
            };

        public Method Build(ITypeSymbol type)
        {
            var arguments = Arguments(type.ToString()).ToImmutableArray();

            return GetMethod(arguments, (bodyWriter) =>
            {
                bodyWriter
                    .WriteVariable("pathArray", $"{arguments[1].Name}.Split(new[] {{ '.' }}, System.StringSplitOptions.RemoveEmptyEntries)")
                    .Write($"Set({arguments[0].Name}, new System.Collections.Generic.Queue<string>(pathArray), {arguments[2].Name}, {arguments[3].Name})")
                    .EndStatement();
            }).WithAttribute(Sharpie.Attribute.InlineAttribute);
        }

        public Method Stub() => GetMethod(Arguments("object"), string.Empty);

        private static Method GetMethod(IEnumerable<Parameter> arguments, Action<BodyWriter> body) => new(Accessibility.Public, true, false, ReturnType, MethodName, arguments, body);
        private static Method GetMethod(IEnumerable<Parameter> arguments, string body) => new(Accessibility.Public, true, false, ReturnType, MethodName, arguments, body);
    }
}
