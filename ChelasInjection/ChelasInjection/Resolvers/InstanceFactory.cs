using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ChelasInjection.Exceptions;

namespace ChelasInjection.Resolvers
{
    internal class InstanceFactory
    {
        internal object ResolveInstance(TypeConfig config, Func<Type, Type, object> injectorGetInstance)
        {
            switch (config.ConstructorType)
            {
                case ConstructorType.DefaultConstructor:
                    break;
                case ConstructorType.MatchedArgumentsConstructor:
                    break;
                case ConstructorType.NoArgumentsConstructor:
                    break;
                case ConstructorType.LongestResolvableConstructor:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #region Helper Methods

        private object GetInstanceByConstructorType(TypeConfig config, Func<TypeConfig, ConstructorInfo> constructorSelector)
        {
            ConstructorInfo constructorInfo = constructorSelector.Invoke(config);

            if (constructorInfo == null)
            {
                throw new MissingAppropriateConstructor();
            }

            return GetInstanceByConstructor(constructorInfo, config);
        }

        private object GetInstanceByConstructor(ConstructorInfo constructorInfo, TypeConfig config)
        {
            ParameterInfo[] constructorParametersInfo = constructorInfo.GetParameters();
            object[] constructorParameters = new object[constructorParametersInfo.Length];
            object instance = null;
            var tuple = new Tuple<Type, Type>(config.TargetType, config.AttributeSelector);

            this.resolvingObjectsRepository.Add(tuple);

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

            switch (config.Lifecycle)
            {
                case Lifecycle.PerRequest:
                    instance = this.resolvedObjectsRepository.ContainsKey(tuple) ? this.resolvedObjectsRepository[tuple] : constructorInfo.Invoke(constructorParameters);
                    break;
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

            this.resolvingObjectsRepository.Remove(tuple);

            if (config.InitializeObjectWith != null)
            {
                config.InitializeObjectWith.Invoke(instance);
            }

            return instance;
        }

        #endregion
    }
}