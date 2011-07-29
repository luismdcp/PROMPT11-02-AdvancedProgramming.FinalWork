using System;
using System.Collections.Generic;
using System.Linq;
using ChelasInjection.Contracts;

namespace ChelasInjection
{
    public delegate object ResolverHandler(Binder sender, Type t);

    public abstract class Binder
    {
        private IDictionary<Tuple<Type, Type>, TypeConfig> bindings;
        private TypeConfig currentConfig;

        public event ResolverHandler CustomResolver;

        public object InvokeCustomResolver(Type t)
        {
            ResolverHandler handler = CustomResolver;
            return handler != null ? handler(this, t) : null;
        }

        protected abstract void InternalConfigure();

        /// <summary>
        /// Método para verificar se uma lista de Type estão registados nos bindings.
        /// </summary>
        /// <param name="argumentsTypes"></param>
        /// <returns></returns>
        public bool AllResolvable(Type[] argumentsTypes)
        {
            return argumentsTypes.Length == 0 || argumentsTypes.All(at => this.bindings.Keys.Any(tp => tp.Item1 == at));
        }

        /// <summary>
        /// Método para obter um binding de um tipo que poderá ou não estar registado.
        /// </summary>
        /// <param name="sourceType">Type do tipo fonte</param>
        /// <param name="atributeType">Type do atributo de decoração de parâmetros (RedAttribute, YellowAttribute)</param>
        /// <returns>TypeConfig associado ao tipo fonte, caso o tipo esteja registado. Caso o tipo não esteja registado 
        /// retorna um TypeConfig inicializado com o tipo não registado</returns>
        internal TypeConfig GetBinding(Type sourceType, Type atributeType)
        {
            var tuple = new Tuple<Type, Type>(sourceType, atributeType);
            TypeConfig config = this.bindings.ContainsKey(tuple) ? this.bindings[tuple] : new TypeConfig(sourceType);
            return config;
        }

        internal void Configure()
        {
            this.bindings = new Dictionary<Tuple<Type, Type>, TypeConfig>();
            this.InternalConfigure();
            this.EndBind();
        }

        public ITypeBinder<TTarget> Bind<TSource, TTarget>()
        {
            EndBind();
            currentConfig = new TypeConfig(typeof(TTarget)) { SourceType = typeof(TSource) };
            return new TypeBinder<TTarget>(currentConfig);
        }

        private void EndBind()
        {
            if (currentConfig != null)
            {
                this.bindings[new Tuple<Type, Type>(currentConfig.SourceType, currentConfig.AttributeSelector)] = currentConfig;
            }
        }

        public ITypeBinder<TSource> Bind<TSource>()
        {
            return this.Bind<TSource, TSource>();
        }
    }
}