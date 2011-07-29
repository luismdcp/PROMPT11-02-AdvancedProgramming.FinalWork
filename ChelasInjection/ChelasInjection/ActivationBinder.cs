
using ChelasInjection.Contracts;

namespace ChelasInjection
{
    /// <summary>
    /// Classe para configurar o ciclo de vida do objecto a retornar.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class ActivationBinder<T> : IActivationBinder<T>
    {
        private readonly TypeConfig config;

        public ActivationBinder(TypeConfig config)
        {
            this.config = config;
        }

        public ITypeBinder<T> PerRequest()
        {
            this.config.Lifecycle = Lifecycle.PerRequest;
            return new TypeBinder<T>(this.config);
        }

        public ITypeBinder<T> Singleton()
        {
            this.config.Lifecycle = Lifecycle.Singleton;
            return new TypeBinder<T>(this.config);
        }
    }
}