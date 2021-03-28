using System;
using System.Reflection;

namespace WrapperMaker.App.IpyObjects
{
    public class PythonField
    {
        public string Name { get; }
        public Type FieldType { get; }
        public bool IsStatic { get; }

        public PythonField(FieldInfo field)
        {
            Name = field.Name;
            FieldType = field.FieldType;
            IsStatic = field.IsStatic;
        }
    }
}
