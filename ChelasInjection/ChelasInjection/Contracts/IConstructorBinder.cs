using System;

namespace ChelasInjection.Contracts 
{
    public interface IConstructorBinder<T> 
    {
        ITypeBinder<T> WithValues(Func<object> values);
    }
}