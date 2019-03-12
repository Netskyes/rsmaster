using System;

namespace RSMaster.Data
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    internal class PropInsertIgnore : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    internal class PropUpdateIgnore : Attribute
    {
    }
}
