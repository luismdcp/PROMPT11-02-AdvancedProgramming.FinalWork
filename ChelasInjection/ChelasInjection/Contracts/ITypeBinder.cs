using System;

namespace ChelasInjection.Contracts
{
    public interface ITypeBinder<T>
    {
        IActivationBinder<T> WithActivation { get; set; }

        IConstructorBinder<T> WithConstructor(params Type[] constructorArguments);
        ITypeBinder<T> WithNoArgumentsConstructor();
        ITypeBinder<T> InitializeObjectWith(Action<T> initialization);
        void WhenArgumentHas<TAttribute>() where TAttribute : Attribute;
    }
}