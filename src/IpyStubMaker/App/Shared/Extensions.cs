using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace WrapperMaker.App.Shared
{
    public static class Extensions
    {
        public static string Repeat(this char s, int x) => new string(s, x);

        public static string ToSnakeCase(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return Regex.Replace(
                value,
                "(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z])",
                "_$1",
                RegexOptions.Compiled)
                .Trim()
                .ToLower();
        }

        public static string ToIronPythonType(this Type csType, bool ignoreArgs = false)
        {
            if (csType.IsGenericType)
                return csType.GetIronPythonGenericType(ignoreArgs);
            if (csType.IsArray)
                return csType.GetIronPythonArrayType();
            return csType.GetIronPythonType();
        }

        public static string ToIronPythonValue(this object csValue)
        {
            if (csValue == null)
                return "None";

            var csType = csValue.GetType();

            if (csType == typeof(string))
                return "\"" + csValue + "\"";

            return csValue.ToString();
        }

        private static string GetIronPythonType(this Type csType)
        {
            return Regex.Replace(csType.Name, @"\W+", string.Empty);
        }

        private static string GetIronPythonGenericType(this Type csType, bool ignoreGenericArgs)
        {
            var result = csType.Name.Split('`').FirstOrDefault();
            var args = csType.GenericTypeArguments.Select(x => x.ToIronPythonType());

            if (!ignoreGenericArgs && args.Any())
                result += $"[{string.Join(", ", args)}]";

            return result;
        }

        private static string GetIronPythonArrayType(this Type csType) =>
            $"Array[{csType.GetElementType().ToIronPythonType()}]";

        public static bool IsValidImport(this Type type)
        {
            var isValid = true;
            isValid = isValid && !type.IsNested;
            return isValid;
        }
    }
}
