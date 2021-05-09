using Microsoft.CodeAnalysis;
using Sharpie;

namespace DynamicPropertyGenerator.Methods
{
    public interface IDynamicMethodBuilder
    {
        Method Build(ITypeSymbol type);
        Method Stub();
    }
}
