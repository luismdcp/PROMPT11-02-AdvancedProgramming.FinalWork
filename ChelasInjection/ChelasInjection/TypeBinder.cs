using System;
using ChelasInjection.Contracts;

namespace ChelasInjection
{
    /// <summary>
    /// Classe para definir as configurações relativas ao constructor a usar, ao ciclo de vida e inicialização da instância criada.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class TypeBinder<T> : ITypeBinder<T>
    {
        private readonly TypeConfig config;

        public IActivationBinder<T> WithActivation
        {
            get { return new ActivationBinder<T>(this.config); }
            set { this.WithActivation = value; }
        }

        public TypeBinder(TypeConfig config)
        {
            this.config = config;
        }

        public IConstructorBinder<T> WithConstructor(params Type[] constructorArguments)
        {
            this.config.ConstructorArgumentsTypes = constructorArguments;
            this.config.ConstructorType = ConstructorType.MatchedArgumentsConstructor;
            return new ConstructorBinder<T>(this.config);
        }

        public ITypeBinder<T> WithNoArgumentsConstructor()
        {
            this.config.ConstructorType = ConstructorType.NoArgumentsConstructor;
            return new TypeBinder<T>(this.config);
        }

        public ITypeBinder<T> InitializeObjectWith(Action<T> initialization)
        {
            this.config.InitializeObjectWith = o => initialization((T) o);
            return new TypeBinder<T>(this.config);
        }

        public void WhenArgumentHas<TAttribute>() where TAttribute : Attribute
        {
            this.config.AttributeSelector = typeof(TAttribute);
        }
    }
}