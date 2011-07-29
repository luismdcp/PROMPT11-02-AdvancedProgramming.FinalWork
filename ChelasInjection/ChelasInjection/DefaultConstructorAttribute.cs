using System;

namespace ChelasInjection
{
    [AttributeUsage(AttributeTargets.Constructor)]
    public class DefaultConstructorAttribute : Attribute
    {
    }
}