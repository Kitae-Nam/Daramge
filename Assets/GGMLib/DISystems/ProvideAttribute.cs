using System;

namespace GGMLib.DISystems
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class ProvideAttribute : Attribute
    {
    }
}