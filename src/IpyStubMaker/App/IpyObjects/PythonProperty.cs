using System;
using System.Reflection;

namespace WrapperMaker.App.IpyObjects
{
    public class PythonProperty
    {
        public string Name { get; }
        public Type PropertyType { get; }
        public bool IsStatic { get; }

        public PythonProperty(PropertyInfo prop)
        {
            Name = prop.Name;
            PropertyType = prop.PropertyType;
            IsStatic = prop.GetMethod?.IsStatic ?? prop.SetMethod.IsStatic;
        }
    }
}
