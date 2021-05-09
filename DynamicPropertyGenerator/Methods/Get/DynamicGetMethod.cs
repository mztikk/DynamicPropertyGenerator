using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Sharpie;
using Sharpie.Writer;

namespace DynamicPropertyGenerator.Methods.Get
{
    internal class DynamicGetMethod : IDynamicMethodBuilder
    {
        private const string MethodName = "Get";
        private const string ReturnType = "object";

        private static Parameter[] Arguments(string type) => new Parameter[]
            {
                new(type, "obj", true),
                new("string", "path"),
                new("bool", "ignoreCasing", "false"),
            };

        public Method Build(ITypeSymbol type)
        {
            var arguments = Arguments(type.ToString()).ToImmutableArray();

            return GetMethod(arguments, (bodyWriter) =>
            {
                bodyWriter
                    .WriteVariable("pathArray", $"{arguments[1].Name}.Split(new[] {{ '.' }}, System.StringSplitOptions.RemoveEmptyEntries)")
                    .WriteReturn($"Get({arguments[0].Name}, new System.Collections.Generic.Queue<string>(pathArray), {arguments[2].Name})");
            }).WithAttribute(Sharpie.Attribute.InlineAttribute);
        }

        public Method Stub() => GetMethod(Arguments("object"), (bodyWriter) => bodyWriter.WriteReturn("new object()"));

        private static Method GetMethod(IEnumerable<Parameter> arguments, Action<BodyWriter> body) => new(Accessibility.Public, true, false, ReturnType, MethodName, arguments, body);
    }
}
