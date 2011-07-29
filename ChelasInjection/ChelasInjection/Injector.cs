using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ChelasInjection.Exceptions;

namespace ChelasInjection
{
    public class Injector
    {
        private readonly Binder myBinder;

        // Contentor de objectos para os casos de ciclo de vida Singleton
        private readonly IDictionary<Tuple<Type, Type>, object> singletonsRepository;

        // Contentor de objectos que foram totalmente resolvidos e criados. Este contentor é usado para 
        // os casos de ciclo de vida PerRequest.
        private readonly IDictionary<Tuple<Type, Type>, object> resolvedObjectsRepository;

        // Contentor de objectos que estão em processo de resolução e ainda não foram criados. Este contentor é usado
        // para a verificação de dependências circulares.
        private readonly IList<Tuple<Type, Type>> resolvingObjectsRepository;

        public Injector(Binder myBinder)
        {
            this.singletonsRepository = new Dictionary<Tuple<Type, Type>, object>();
            this.resolvedObjectsRepository = new Dictionary<Tuple<Type, Type>, object>();
            this.resolvingObjectsRepository = new List<Tuple<Type, Type>>();
            this.myBinder = myBinder;
            this.myBinder.Configure();
        }

        public T GetInstance<T>()
        {
            this.resolvedObjectsRepository.Clear();
            return (T) this.GetInstance(typeof(T), null);
        }

        public T GetInstance<T, TA>()
        {
            this.resolvedObjectsRepository.Clear();
            return (T) this.GetInstance(typeof(T), typeof(TA));
        }

        #region Helper Methods

        /// <summary>
        /// Método auxiliar para criar um objecto a partir de uma configuração e um constructor.
        /// </summary>
        /// <param name="config">TypeConfig que contém toda a configuração definida para a criação do objecto</param>
        /// <param name="constructorSelector">Método para escolher e devolver o constructor apropriado</param>
        /// <returns>Instância do TargetType definido no objecto TypeConfig</returns>
        private object GetInstanceByConstructorType(TypeConfig config, Func<TypeConfig, ConstructorInfo> constructorSelector)
        {
            ConstructorInfo constructorInfo = constructorSelector.Invoke(config);

            // caso não exista um constructor apropriado ao caso lançar excepção
            if (constructorInfo == null)
            {
                throw new MissingAppropriateConstructor();
            }

            return GetInstanceByConstructor(constructorInfo, config);
        }

        /// <summary>
        /// Método auxiliar para criação da instância do tipo registado para o sourceType e atributeType
        /// </summary>
        /// <param name="sourceType">Type do tipo fonte</param>
        /// <param name="atributeType">Type do atributo RedAttribute ou YellowAttribute ou nulo</param>
        /// <returns>Instância do tipo registado</returns>
        private object GetInstance(Type sourceType, Type atributeType)
        {
            // se o tipo do objecto estiver no contentor dos tipos que ainda estão em processo de resolução 
            // então lançar excepção de dependência circular.
            if (this.resolvingObjectsRepository.Contains(new Tuple<Type, Type>(sourceType, atributeType)))
            {
                throw new CircularDependencyException();
            }

            // obtém o binding registado para o sourceType e atributeType
            TypeConfig config = this.myBinder.GetBinding(sourceType, atributeType);

            // se o tipo que se está a tentar resolver não for uma classe entao lançar excepção
            if (!config.TargetType.IsClass)
            {
                throw new UnboundTypeException(String.Format("Type '{0}' can not be resolved and instantiated.", config.TargetType.FullName));
            }

            object instance = GetInstanceByConfig(config);
            var tuple = new Tuple<Type, Type>(instance.GetType(), config.AttributeSelector);

            // guarda informação sobre o tipo no contentor de tipos já resolvidos para ser usado nos casos
            // de ciclo de vida PerRequest.
            if (!this.resolvedObjectsRepository.ContainsKey(tuple))
            {
                this.resolvedObjectsRepository.Add(tuple, instance);
            }
            
            return instance;
        }

        private object GetInstanceByConfig(TypeConfig config)
        {
            object instance = null;

            // casos possíveis de resolução do tipo através do constructor
            switch (config.ConstructorType)
            {
                // caso do constructor decorado com o atributo DefaultAttribute
                case ConstructorType.DefaultConstructor:
                    ConstructorInfo defaultConstructorInfo = this.GetDefaultConstructor(config.TargetType);

                    if (defaultConstructorInfo != null)
                    {
                        instance = GetInstanceByConstructor(defaultConstructorInfo, config);
                    }
                    else
                    {
                        // se não existir um constructor decorado com o atributo DefaultAttribute
                        // então tentar o caso do constructor com o maior número de parâmetros possíveis de resolver.
                        goto case ConstructorType.LongestResolvableConstructor;
                    }

                    break;
                // caso do constructor que faça match com a lista de Types de argumentos
                case ConstructorType.MatchedArgumentsConstructor:
                    instance = this.GetInstanceByConstructorType(config, this.GetMatchedArgumentsConstructorInfo);
                    break;
                // caso do constructor sem parâmetros
                case ConstructorType.NoArgumentsConstructor:
                    instance = this.GetInstanceByConstructorType(config, this.GetNoArgumentsConstructorInfo);
                    break;
                // caso do constructor com o maior número de parâmetros possíveis de resolver
                case ConstructorType.LongestResolvableConstructor:
                    instance = this.GetInstanceByConstructorType(config, this.GetLongestResolvableConstructorInfo);
                    break;
            }

            return instance;
        }

        private object GetInstanceByConstructor(ConstructorInfo constructorInfo, TypeConfig config)
        {
            ParameterInfo[] constructorParametersInfo = constructorInfo.GetParameters();
            object[] constructorParameters = new object[constructorParametersInfo.Length];
            object instance = null;

            var tuple = new Tuple<Type, Type>(config.TargetType, config.AttributeSelector);

            // adiciona ao contentor dos objectos por resolver o tuplo com o Type do tipo e o Type do atributo (RedAttribute, YellowAttribute, null)
            this.resolvingObjectsRepository.Add(tuple);

            // resolve os parâmetros do constructor
            this.BuildConstructorParameters(constructorParametersInfo, config, constructorParameters);

            switch (config.Lifecycle)
            {
                // caso a configuração indique um ciclo de vida PerRequest devolve a instância caso já tenha sido resolvida
                // senão invoca o constructor com os parâmetros anteriormente resolvidos
                case Lifecycle.PerRequest:
                    instance = this.resolvedObjectsRepository.ContainsKey(tuple) ? this.resolvedObjectsRepository[tuple] : constructorInfo.Invoke(constructorParameters);
                    break;
                // caso a configuração indique um ciclo de vida Singleton devolve a instância caso exista no contentor de singletons
                // senao invoca o constructor com os parâmetros anteriormente resolvidos
                case Lifecycle.Singleton:
                    if (this.singletonsRepository.ContainsKey(tuple))
                    {
                        instance = this.singletonsRepository[tuple];
                    }
                    else
                    {
                        instance = constructorInfo.Invoke(constructorParameters);
                        this.singletonsRepository.Add(tuple, instance);
                    }

                    break;
            }

            // remove o tuplo anteriormente definido com os Type do contentor dos objectos em resolução
            // para não ser lançada uma excepção de dependência circular.
            this.resolvingObjectsRepository.Remove(tuple);

            if (config.InitializeObjectWith != null)
            {
                config.InitializeObjectWith.Invoke(instance);
            }

            return instance;
        }

        /// <summary>
        /// Método auxiliar para obter uma lista de valores de inicialização de parâmetros de um constructor.
        /// </summary>
        /// <param name="constructorParametersInfo"> Lista de ParameterInfo relativos aos parâmetros de um constructor</param>
        /// <param name="config">TypeConfig com a configuração de inicialização de parâmetros do constructor  </param>
        /// <param name="constructorParameters">Lista para afectação dos valores de inicialização dos parâmetros</param>
        private void BuildConstructorParameters(ParameterInfo[] constructorParametersInfo, TypeConfig config, object[] constructorParameters)
        {
            for (int i = 0; i < constructorParameters.Length; i++)
            {
                object parameterValue = this.GetParameterValueFromPropertyInfo(constructorParametersInfo[i], config);

                if (parameterValue != null)
                {
                    constructorParameters[i] = parameterValue;
                }
                else
                {
                    Type parameterType = constructorParametersInfo[i].ParameterType;
                    Type colorAttributeType = this.GetColorAtributeFromParameter(constructorParametersInfo[i]);

                    constructorParameters[i] = this.GetInstance(parameterType, colorAttributeType); 
                }
            }
        }

        /// <summary>
        /// Método auxiliar para obter o Type do atributo RedAttribute ou YellowAttribute de um parâmetro
        /// </summary>.
        /// <param name="parameterInfo">ParameterInfo do parâmetro</param>
        /// <returns>Type do atributo RedAttribute ou YellowAttribute caso estejam a decorar o parâmetro</returns>
        private Type GetColorAtributeFromParameter(ParameterInfo parameterInfo)
        {
            // Nota: foram usados os nomes dos atributos e não os Type porque ao fazer referência ao projecto ChelasInjection.SampleTypes
            // dava circular reference e como tal o ideal seria ter os atributos RedAttribute e YellowAttribute definidos no projecto
            // ChelasInjection, já que o seu uso é muito semelhante ao atributo DefaultAttribute.
            var colorAttribute = parameterInfo.GetCustomAttributes(true)
                                              .Where(ca => ca.GetType().Name == "RedAttribute" | ca.GetType().Name == "YellowAttribute")
                                              .FirstOrDefault();

            return colorAttribute != null ? colorAttribute.GetType() : null;
        }

        /// <summary>
        /// Método auxiliar para obter o constructor com o maior número de parâmetros possíveis de resolver.
        /// </summary>
        /// <param name="config">TypeConfig com a configuração do tipo</param>
        /// <returns></returns>
        private ConstructorInfo GetLongestResolvableConstructorInfo(TypeConfig config)
        {
            ConstructorInfo longestResolvableConstructor = config.TargetType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                                                          .Where(c => myBinder.AllResolvable(c.GetParameters().Select(pi => pi.ParameterType).ToArray()))
                                                          .OrderByDescending(c => c.GetParameters().Length)
                                                          .FirstOrDefault();

            if (longestResolvableConstructor == null)
            {
                throw new MissingAppropriateConstructor();
            }

            return longestResolvableConstructor;
        }

        /// <summary>
        /// Método auxiliar para obter o constructor cujos Types dos parâmetros façam match com os Types definidos na configuração.
        /// </summary>
        /// <param name="config">TypeConfig com a configuração dos Types para matching na propriedade ConstructorArgumentsTypes da configuração</param>
        /// <returns></returns>
        private ConstructorInfo GetMatchedArgumentsConstructorInfo(TypeConfig config)
        {
            return config.TargetType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                                    .Where(c => c.GetParameters().Select(pi => pi.ParameterType).SequenceEqual(config.ConstructorArgumentsTypes))
                                    .FirstOrDefault();
        }

        /// <summary>
        /// Método auxiliar para obter o constructor sem parâmetros do tipo definido na configuração.
        /// </summary>
        /// <param name="config">TypeConfig com a configuração do tipo</param>
        /// <returns>ConstructorInfo do constructor sem parâmetros</returns>
        private ConstructorInfo GetNoArgumentsConstructorInfo(TypeConfig config)
        {
            return config.TargetType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                                    .Where(c => c.GetParameters().Length == 0)
                                    .FirstOrDefault();
        }

        /// <summary>
        /// Método auxiliar para obter o constructor decorado com o atributo DefaultAttribute.
        /// </summary>
        /// <param name="targetType">Type do tipo do qual se quer obter o constructor decorado com o atributo DefaultAttribute</param>
        /// <returns>ConstructorInfo relativo ao constructor decorado com o atributo DefaultAttribute</returns>
        private ConstructorInfo GetDefaultConstructor(Type targetType)
        {
            var constructorsWithDefaultAttribute = targetType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                                                             .Where(c => c.GetCustomAttributes(true).Any(ca => ca.GetType() == typeof (DefaultConstructorAttribute)));

            // se houver mais do que um constructor decorado com o atributo DefaultAttribute lançar excepção
            if (constructorsWithDefaultAttribute.Count() > 1)
            {
                throw new MultipleDefaultConstructorAttributesException();
            }

            return constructorsWithDefaultAttribute.FirstOrDefault();
        }

        /// <summary>
        /// Método auxiliar para obter um valor de inicialização de um parâmetro.
        /// </summary>
        /// <param name="parameterInfo">ParameterInfo relativo ao parâmetro que se quer inicializar</param>
        /// <param name="typeConfig">TypeConfig com a informação de configuração de inicialização de parâmetros</param>
        /// <returns></returns>
        private object GetParameterValueFromPropertyInfo(ParameterInfo parameterInfo, TypeConfig typeConfig)
        {
            if (typeConfig.PropertiesInfos == null)
            {
                return null;
            }

            // obtém o PropertyInfo com o mesmo Type que o paramêtro que se quer inicializar.
            PropertyInfo propertyInfo = typeConfig.PropertiesInfos.Where(pi => pi.PropertyType == parameterInfo.ParameterType).FirstOrDefault();
            return propertyInfo != null ? propertyInfo.GetValue(typeConfig.ObjectWithPropertiesInitializer, null) : null;
        }

        #endregion
    }
}