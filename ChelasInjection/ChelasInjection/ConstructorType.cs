
namespace ChelasInjection
{
    /// <summary>
    /// Enumerado para definir qual o constructor a usar para criação da instância
    /// </summary>
    internal enum ConstructorType
    {
        DefaultConstructor,             // Constructor decorado com o atributo DefaultAttribute
        MatchedArgumentsConstructor,    // Constructor cujos parâmetros façam match com uma lista de Type
        NoArgumentsConstructor,         // Constructor sem parâmetros
        LongestResolvableConstructor    // Constructor com o maior número de parâmetros registados nos bindings e possíveis de instanciar
    }
}