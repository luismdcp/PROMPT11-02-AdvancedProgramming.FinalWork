using System;
using System.Runtime.Serialization;

namespace ChelasInjection.Exceptions
{
    /// <summary>
    /// Excepção lançada quando não existe um contructor para criação de uma instância do tipo pretendido.
    /// </summary>
    [Serializable]
    public class MissingAppropriateConstructor : Exception
    {
        public MissingAppropriateConstructor()
        {
        }

        public MissingAppropriateConstructor(string message) : base(message)
        {
        }

        public MissingAppropriateConstructor(string message, Exception inner) : base(message, inner)
        {
        }

        protected MissingAppropriateConstructor(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}