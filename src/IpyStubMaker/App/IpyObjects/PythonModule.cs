using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WrapperMaker.App.Shared;

namespace WrapperMaker.App.IpyObjects
{
    public class PythonModule
    {
        public string AssemblyName { get; }
        public string Namespace { get; }
        public List<PythonClass> Classes { get; }

        public PythonModule(IEnumerable<Type> types, string nameSpace)
        {
            AssemblyName = types.FirstOrDefault()?.Assembly.FullName;
            Namespace = nameSpace;
            Classes = new List<PythonClass>();

            ConfigureClasses(types);
        }

        private void ConfigureClasses(IEnumerable<Type> types)
        {
            foreach (var cs in types.Where(x => x.Namespace == Namespace))
            {
                var pyClass = new PythonClass(cs);
                Classes.Add(pyClass);
            }
        }

        public string ModuleStubToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# [{Namespace}] - {AssemblyName}");
            foreach (var className in Classes.Select(x => x.ClassType.ToIronPythonType()).Distinct())
                sb.AppendLine($"from .__init__parts.{className} import {className}");
            sb.AppendLine();
            return sb.ToString();
        }
    }
}
