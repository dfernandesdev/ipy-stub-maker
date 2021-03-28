using System;
using System.Reflection;

namespace WrapperMaker.App.IpyObjects
{
    public class PythonParameter
    {
        public string Name { get; }
        public Type ParameterType { get; }
        public bool IsOptional { get; }
        public object DefaultValue { get; }

        public PythonParameter(ParameterInfo parameter)
        {
            Name = parameter.Name;
            ParameterType = parameter.ParameterType;
            IsOptional = parameter.IsOptional;
            DefaultValue = parameter.DefaultValue;
        }

        public PythonParameter(string name, Type paramType, bool optional = false, object value = null)
        {
            Name = name;
            ParameterType = paramType;
            IsOptional = optional;
            DefaultValue = value;
        }
    }
}
