using System;
using System.Linq;
using System.Reflection;
using ChelasInjection.Contracts;

namespace ChelasInjection
{
    /// <summary>
    /// Classe para pré-definir valores dos argumentos do constructor, através de objectos anónimos
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class ConstructorBinder<T> : IConstructorBinder<T>
    {
        private readonly TypeConfig config;

        public ConstructorBinder(TypeConfig config)
        {
            this.config = config;
        }

        public ITypeBinder<T> WithValues(Func<object> values)
        {
            object invokeReturn = values();
            var propertiesInfo = invokeReturn.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            this.config.PropertiesInfos = propertiesInfo.ToList();
            this.config.ObjectWithPropertiesInitializer = invokeReturn;
            return new TypeBinder<T>(this.config);
        }
    }
}