using System.Linq;
using Microsoft.CodeAnalysis;

namespace DynamicPropertyGenerator.Extensions
{
    public static class ISymbolExtensions
    {
        private const string ObsoleteAttribute = "ObsoleteAttribute";
        public static bool IsObsolete(this ISymbol symbol) => symbol.GetAttributes().Any(y => y.AttributeClass?.Name.Equals(ObsoleteAttribute) == true);
    }
}
