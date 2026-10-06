using System;

namespace GGMLib.DISystems
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field)]
    public sealed class InjectAttribute : Attribute
    {
    }
}