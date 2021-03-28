using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using WrapperMaker.App.IpyObjects;
using WrapperMaker.App.Shared;

namespace WrapperMaker
{
    class Program
    {
        private static string BaseDir = null;
        private static string StubsDir = null;
        private static Assembly LoadedAssembly = null;
        private static List<Type> ExportedTypes = null;
        private static Stopwatch Stopwatch = new Stopwatch();

        static void Main(string[] args)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;

            ValidateArgs(args);

            Stopwatch.Restart();
            Console.Write($"Loading Assembly...");
            LoadedAssembly = Assembly.LoadFrom(args[0]);
            Console.Write($"- Done! ({Stopwatch.ElapsedMilliseconds}ms)\n");

            BaseDir = Directory.GetCurrentDirectory();
            BaseDir = args.Length == 2 ? args[1] : BaseDir;
            StubsDir = BaseDir + @"\stubs\";

            ExportedTypes = LoadedAssembly.ExportedTypes.Where(x => x.IsPublic && x.BaseType != typeof(Form)).ToList();

            Console.WriteLine($"Starting...");
            Console.WriteLine();
            foreach (var ns in ExportedTypes.Select(x => x.Namespace).Distinct())
            {
                var module = new PythonModule(ExportedTypes, ns);
                if (module.Classes.Any())
                {
                    ExportModuleInit(module);
                    ExportClasses(module);
                    Console.WriteLine();
                }
            }
            Console.WriteLine($"Finished!");
            Console.Read();
        }

        private static void ExportModuleInit(PythonModule module)
        {
            var rootDir = $"{StubsDir}{module.Namespace.Replace(".", "\\")}";
            Directory.CreateDirectory(rootDir);
            File.AppendAllText($@"{rootDir}\__init__.py", module.ModuleStubToString());
        }

        private static void ExportClasses(PythonModule module)
        {
            var rootDir = $"{StubsDir}{module.Namespace.Replace(".", "\\")}";
            var partsDir = $@"{rootDir}\__init__parts";
            Directory.CreateDirectory(partsDir);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[{module.Namespace}]");
            Console.ForegroundColor = ConsoleColor.White;

            foreach (var cs in module.Classes)
            {
                Stopwatch.Restart();
                Console.Write($"[{cs.ClassType.ToIronPythonType()}] - Exporting... ");

                File.WriteAllText($@"{partsDir}\{cs.ClassType.ToIronPythonType()}.py", cs.ClassStubToString());

                if (cs.FieldsErrors.Any() || cs.PropertiesErrors.Any() || cs.MethodsErrors.Any())
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(" Erros: ");
                    Console.Write(cs.FieldsErrors.Any() ? $"> Fields: [{string.Join(", ", cs.FieldsErrors)}] " : "");
                    Console.Write(cs.PropertiesErrors.Any() ? $"> Properties: [{string.Join(", ", cs.PropertiesErrors)}] " : "");
                    Console.Write(cs.MethodsErrors.Any() ? $"> Methods: [{string.Join(", ", cs.MethodsErrors)}] " : "");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                Console.Write($"- Done! ({Stopwatch.ElapsedMilliseconds}ms)\n");

            }
        }

        private static void ValidateArgs(string[] args)
        {
            if (args.Length == 0)
                throw new ArgumentException("Please informe the path to the assembly.");
            if (!File.Exists(args[0]))
                throw new FileNotFoundException("The assembly file informed does not exist.");

        }
    }
}
