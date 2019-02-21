using System;

namespace RSMaster.Extensions
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public class PropInsertIgnore : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public class PropUpdateIgnore : Attribute
    {
    }
}
