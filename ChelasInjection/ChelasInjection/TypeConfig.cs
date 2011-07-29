using System;
using System.Collections.Generic;
using System.Reflection;

namespace ChelasInjection
{
    /// <summary>
    /// Classe para guardar toda a informação de configuração do tipo.
    /// </summary>
    internal class TypeConfig
    {
        public Type SourceType { get; set; }                        
        public Type TargetType { get; set; }
        public Lifecycle Lifecycle { get; set; }

        // lista de Type relativos a cada argumento do constructor, para guardar a  informação da chamada ao método WithConstructor da classe TypeBinder
        public Type[] ConstructorArgumentsTypes { get; set; }

        // lista de PropertyInfo relativos a cada Propriedade do objecto anónimo retornado pela chamada ao método WithValues da classe ConstructorBinder
        public IList<PropertyInfo> PropertiesInfos { get; set; }

        // objecto anónimo retornado pela chamada ao método WithValues da classe ConstructorBinder
        public object ObjectWithPropertiesInitializer { get; set; }

        // delegate que encapsula o código fornecido como parâmetro ao método InitializeObjectWith da classe TypeBinder
        public Action<object> InitializeObjectWith { get; set; }

        // Type do atributo do método genérico WhenArgumentHas<T> da classe TypeBinder
        public Type AttributeSelector { get; set; }

        // Valor do enumerado que indica qual o constructor a usar na criação de uma instância
        public ConstructorType ConstructorType { get; set; }

        public TypeConfig(Type targetType)
        {
            this.TargetType = targetType;
            this.Lifecycle = Lifecycle.PerRequest;

            // inicializa a configuração do tipo para, por omissão, usar o constructor decorado com o atributo DefaultAttribute
            this.ConstructorType = ConstructorType.DefaultConstructor;
        }
    }
}