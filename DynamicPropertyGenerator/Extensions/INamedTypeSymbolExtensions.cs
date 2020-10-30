using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace DynamicPropertyGenerator.Extensions
{
    public static class INamedTypeSymbolExtensions
    {
        public static IEnumerable<IPropertySymbol> GetAccessibleProperties(this INamedTypeSymbol symbol) => symbol.GetMembers()
                                                                                                        .OfType<IPropertySymbol>()
                                                                                                        .Where(x => !x.IsReadOnly
                                                                                                                    && x.SetMethod is { }
                                                                                                                    && x.SetMethod.DeclaredAccessibility == Accessibility.Public
                                                                                                                    && x.GetMethod is { }
                                                                                                                    && x.GetMethod.DeclaredAccessibility == Accessibility.Public
                                                                                                                    && !x.IsIndexer
                                                                                                                    && !x.IsStatic
                                                                                                                    && x.Type.TypeKind != TypeKind.Pointer
                                                                                                                    && !x.IsObsolete());

        public static bool HasMatchingProperties(this INamedTypeSymbol left, INamedTypeSymbol right)
        {
            foreach (IPropertySymbol item in left.GetAccessibleProperties())
            {
                bool matching = right.GetAccessibleProperties().Any(x => x.Name == item.Name && SymbolEqualityComparer.Default.Equals(x.Type, item.Type));
                if (matching)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
