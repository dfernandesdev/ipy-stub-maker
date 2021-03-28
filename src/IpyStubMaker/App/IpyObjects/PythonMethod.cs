using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using WrapperMaker.App.Shared;

namespace WrapperMaker.App.IpyObjects
{
    public class PythonMethod
    {
        public string Name { get; }
        public Type ReturnType { get; }
        public List<PythonParameter> Parameters { get; }
        public bool IsStatic { get; }
        public bool IsConstructor { get; }
        public bool IsGenericMethod { get; }
        public bool IsGenericMethodDefinition { get; }

        public PythonMethod(MethodInfo method)
        {
            Name = method.Name;
            ReturnType = method.ReturnType;
            Parameters = new List<PythonParameter>();
            IsStatic = method.IsStatic;
            IsGenericMethod = method.IsGenericMethod;
            IsGenericMethodDefinition = method.IsGenericMethodDefinition;
            if (!IsStatic)
                Parameters.Add(new PythonParameter("self", method.DeclaringType));
            foreach (var p in method.GetParameters())
                Parameters.Add(new PythonParameter(p));
        }

        public PythonMethod(ConstructorInfo ctor)
        {
            Name = "__init__";
            ReturnType = typeof(void);
            Parameters = new List<PythonParameter>();
            IsConstructor = true;
            Parameters.Add(new PythonParameter("self", ctor.DeclaringType));
            foreach (var p in ctor.GetParameters())
                Parameters.Add(new PythonParameter(p));
        }

        public string MethodToString(int identLevel = 0)
        {
            var headIndent = ' '.Repeat(identLevel);
            var bodyIndent = ' '.Repeat(identLevel + 4);
            var parameters = string.Join(", ", Parameters.Select(x => x.IsOptional ? $"{x.Name} = {x.DefaultValue.ToIronPythonValue()}" : x.Name));
            var paramTypes = IsConstructor || !IsStatic ? Parameters.Skip(1).Select(x => x.ParameterType) : Parameters.Select(x => x.ParameterType);
            var annotation = $"# type: ({string.Join(", ", paramTypes.Select(x => x.ToIronPythonType()))}) -> {ReturnType.ToIronPythonType()}";
            var sb = new StringBuilder();
            if (IsStatic)
                sb.AppendLine($"{headIndent}@staticmethod");
            sb.AppendLine($"{headIndent}def {Name}({parameters}):");
            sb.AppendLine($"{bodyIndent}{annotation}");
            sb.Append($"{bodyIndent}pass");
            return sb.ToString();
        }

    }
}
