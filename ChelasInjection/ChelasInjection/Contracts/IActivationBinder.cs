namespace ChelasInjection.Contracts
{
    public interface IActivationBinder<T>
    {
        ITypeBinder<T> PerRequest();
        ITypeBinder<T> Singleton();
    }
}