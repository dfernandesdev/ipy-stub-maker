using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TypeSupport.Extensions;
using WrapperMaker.App.Shared;

namespace WrapperMaker.App.IpyObjects
{
    public class PythonClass
    {

        public Type ClassType { get; }
        public bool IsStatic { get; }
        public bool IsGenericType { get; }
        public bool IsGenericTypeDefinition { get; }
        public Dictionary<string, HashSet<string>> Imports { get; set; }
        public List<Type> Inheritances { get; }
        public List<PythonField> Fields { get; }
        public List<PythonProperty> Properties { get; }
        public List<PythonClass> SubClasses { get; }
        public List<PythonMethod> Methods { get; }
        public HashSet<string> FieldsErrors { get; }
        public HashSet<string> PropertiesErrors { get; }
        public HashSet<string> MethodsErrors { get; }

        public PythonClass(Type csClass)
        {
            ClassType = csClass;
            IsStatic = csClass.IsSealed && csClass.IsAbstract;
            Imports = new Dictionary<string, HashSet<string>>();
            Inheritances = new List<Type>();
            Fields = new List<PythonField>();
            Properties = new List<PythonProperty>();
            Methods = new List<PythonMethod>();
            SubClasses = new List<PythonClass>();
            FieldsErrors = new HashSet<string>();
            PropertiesErrors = new HashSet<string>();
            MethodsErrors = new HashSet<string>();

            ConfigureFields(csClass);
            ConfigureProperties(csClass);
            ConfigureMethods(csClass);
            ConfigureSubClasses(csClass);
            ConfigureInheritances();
            ConfigureImports();
        }

        private void ConfigureFields(Type csClass)
        {
            foreach (var f in csClass.GetFields().Where(x => x.DeclaringType == csClass && x.IsPublic))
                Fields.Add(new PythonField(f));
        }

        private void ConfigureProperties(Type csClass)
        {
            foreach (var p in csClass.GetProperties().Where(x => x.DeclaringType == csClass))
                if ((p.GetMethod != null && p.GetMethod.IsPublic) || (p.SetMethod != null && p.SetMethod.IsPublic))
                    Properties.Add(new PythonProperty(p));
        }

        private void ConfigureMethods(Type csClass)
        {
            foreach (var c in csClass.GetConstructors().Where(x => x.DeclaringType == csClass && x.IsPublic))
                Methods.Add(new PythonMethod(c));

            foreach (var m in csClass.GetMethods().Where(x => x.DeclaringType == csClass && !x.IsSpecialName && x.IsPublic))
            {
                try
                {
                    Methods.Add(new PythonMethod(m));
                }
                catch (Exception)
                {
                    MethodsErrors.Add(m.Name);
                }
            }
        }

        private void ConfigureSubClasses(Type csClass)
        {
            foreach (var cs in csClass.GetNestedTypes().Where(x => x.IsClass || x.IsInterface || x.GetExtendedType().IsStruct))
            {
                var pyClass = new PythonClass(cs);
                SubClasses.Add(pyClass);
            }
        }

        private void ConfigureImports()
        {
            if (Methods.GroupBy(x => x.Name).SelectMany(g => g.Skip(1)).Any())
                AddImport("typing", "overload");
            if (GetGenericTypes().Any())
                AddImport("typing", "TypeVar");

            Inheritances.ForEach(x => AddImport(x));
            Fields.ForEach(x => AddImport(x.FieldType));
            Properties.ForEach(x => AddImport(x.PropertyType));
            Methods.ForEach(x => AddImport(x.ReturnType));
            Methods.SelectMany(x => x.Parameters.Where(y => y.Name != "self"))
                .ToList().ForEach(x => AddImport(x.ParameterType));
        }

        private void ConfigureInheritances()
        {
            var imports = ClassType.GetInterfaces().ToList();
            var index = 0;
            while (imports.Any())
            {
                if (index == imports.Count)
                    break;
                imports.AddRange(imports[index].GetInterfaces());
                index++;
            }

            var bases = ClassType.GetExtendedType().BaseTypes.ToList();
            index = 0;
            while (bases.Any())
            {
                if (index == bases.Count)
                    break;
                bases.AddRange(bases[index].GetInterfaces());
                index++;
            }

            imports = imports.Reverse<Type>().Distinct().Reverse().ToList();
            bases = bases.Reverse<Type>().Distinct().Reverse().ToList();

            Inheritances.AddRange(imports.Union(bases).Distinct().ToList());
        }

        private void AddImport(Type reference)
        {
            var exType = reference.GetExtendedType();

            if (exType.IsArray || exType.IsCollection)
                AddImport(exType.ElementType);

            if (exType.IsArray)
                reference = reference.BaseType;

            if (reference.IsValidImport())
                AddImport(reference.Namespace, reference.ToIronPythonType(true));
        }

        private void AddImport(string ns, string type)
        {
            if (Imports.ContainsKey(ns))
                Imports[ns].Add(type);
            else
                Imports[ns] = new HashSet<string>() { type };
        }

        private List<Type> GetGenericTypes()
        {
            var generics = ClassType.GetGenericArguments().ToList();
            generics.AddRange(ClassType.GetMethods().Where(x => x.DeclaringType == ClassType && x.IsPublic).SelectMany(x => x.GetGenericArguments()));
            return generics.GroupBy(x => x.Name).Select(x => x.First()).ToList();
        }

        private Dictionary<string, HashSet<string>> GetAllImports()
        {
            var generics = GetGenericTypes();
            var resultImports = new Dictionary<string, HashSet<string>>();
            var allImports = Imports.Union(SubClasses.SelectMany(x => x.Imports));
            foreach (var impModule in allImports)
            {
                foreach (var impClass in impModule.Value)
                {
                    if (generics.Any(y => y.Name == impClass))
                        continue;

                    if (resultImports.ContainsKey(impModule.Key))
                        resultImports[impModule.Key].Add(impClass);
                    else
                        resultImports[impModule.Key] = new HashSet<string>() { impClass };
                }
            }
            return resultImports;
        }

        private bool HasBody() =>
            Fields.Any() || Properties.Any() || Methods.Any() || SubClasses.Any();

        public string ClassToString()
        {
            var sb = new StringBuilder();
            var inherits = string.Join(", ", Inheritances);
            var overloads = Methods.GroupBy(x => x.Name).SelectMany(g => g.Skip(1));

            sb.AppendLine($"class {ClassType.Name}({inherits}):");
            foreach (var m in Methods)
            {
                if (overloads.Any(x => x.Name == m.Name))
                    sb.AppendLine($"\t@overload");
                sb.AppendLine(m.MethodToString());
                sb.AppendLine();
            }
            sb.AppendLine();
            return sb.ToString();
        }

        public string ClassStubToString(int identLevel = 0)
        {
            var headIndent = ' '.Repeat(identLevel);
            var bodyIndent = ' '.Repeat(identLevel + 4);
            var sb = new StringBuilder();
            var inherits = string.Join(", ", Inheritances.Select(x => x.ToIronPythonType()));

            if (!ClassType.IsNested)
            {
                foreach (var imp in GetAllImports())
                    sb.AppendLine($"from {imp.Key} import {string.Join(", ", imp.Value)}");
                sb.AppendLine();

                foreach (var g in GetGenericTypes())
                    sb.AppendLine($"{g.Name} = TypeVar('{g.Name}')");
                sb.AppendLine();
            }

            sb.AppendLine($"{headIndent}class {ClassType.ToIronPythonType()}({inherits}):");

            if (HasBody())
            {
                foreach (var cs in SubClasses)
                    sb.Append(cs.ClassStubToString(identLevel + 4));

                foreach (var p in Fields)
                    sb.AppendLine($"{bodyIndent}{p.Name}: {p.FieldType.ToIronPythonType()} # Attribute");

                foreach (var p in Properties)
                    sb.AppendLine($"{bodyIndent}{p.Name}: {p.PropertyType.ToIronPythonType()} # Property");

                var overloads = Methods.GroupBy(x => x.Name).SelectMany(g => g.Skip(1));
                foreach (var m in Methods)
                {
                    if (overloads.Any(x => x.Name == m.Name))
                        sb.AppendLine($"{bodyIndent}@overload");
                    sb.AppendLine(m.MethodToString(identLevel + 4));
                }
            }
            else
            {
                sb.Append($"{bodyIndent}pass");
            }

            return sb.ToString();
        }
    }
}
