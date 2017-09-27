namespace Expresso
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Expresso.Utils;

    /// <summary>
    /// Класс утилит для работы с типами
    /// </summary>
    public static class TypeUtils
    {
        /// <summary>
        /// Значение является значением по умолчанию для типа
        /// </summary>
        /// <param name="type">Тип</param>
        /// <param name="value">Значение</param>
        /// <returns>Признак значения по умолчанию</returns>
        public static bool IsDefault(Type type, object value)
        {
            ArgumentChecker.NotNull(type, nameof(type));

            if (!type.IsValueType)
            {
                return value == null;
            }

            return Equals(Activator.CreateInstance(type), value);
        }

        /// <summary>
        /// Тип может принимать пустые значения
        /// </summary>
        /// <param name="type">Тип</param>
        /// <returns>Признак</returns>
        public static bool IsNullable(Type type)
        {
            return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
        }

        /// <summary>
        /// Получить тип из определения <c>Nullable</c> или его самого
        /// </summary>
        /// <param name="type">Тип</param>
        /// <returns>Приведенный тип</returns>
        public static Type GetUnderlyingTypeOrSelf(Type type)
        {
            return Nullable.GetUnderlyingType(type) ?? type;
        }

        /// <summary>
        /// Проверка является ли тип простым <c>(int, char etc.)</c>
        /// </summary>
        /// <param name="type">Проверяемый тип</param>
        /// <returns><c>True - тип простой, false - в противном случае</c></returns>
        public static bool IsSimpleType(this Type type)
        {
            var code = Type.GetTypeCode(GetUnderlyingTypeOrSelf(type));

            switch (code)
            {
                case TypeCode.Object:
                    return false;

                default:
                    return true;
            }
        }

        /// <summary>
        /// Возвращает принятое в C# имя типа
        /// </summary>
        /// <param name="type">Тип</param>
        /// <returns>Строковое представление типа</returns>
        internal static string CSharpTypeName(Type type)
        {
            if (type == typeof(bool))
            {
                return "bool";
            }
            if (type == typeof(byte))
            {
                return "byte";
            }
            if (type == typeof(sbyte))
            {
                return "sbyte";
            }
            if (type == typeof(char))
            {
                return "char";
            }
            if (type == typeof(decimal))
            {
                return "decimal";
            }
            if (type == typeof(double))
            {
                return "double";
            }
            if (type == typeof(float))
            {
                return "float";
            }
            if (type == typeof(int))
            {
                return "int";
            }
            if (type == typeof(uint))
            {
                return "uint";
            }
            if (type == typeof(long))
            {
                return "long";
            }
            if (type == typeof(ulong))
            {
                return "ulong";
            }
            if (type == typeof(object))
            {
                return "object";
            }
            if (type == typeof(short))
            {
                return "short";
            }
            if (type == typeof(ushort))
            {
                return "ushort";
            }
            if (type == typeof(string))
            {
                return "string";
            }
            if (type == typeof(void))
            {
                return "void";
            }
            if (type.IsGenericType && type != typeof(Nullable<>) && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return CSharpTypeName(type.GetGenericArguments().Single()) + "?";
            }
            if (type.IsGenericParameter)
            {
                return type.Name;
            }
            if (type.IsArray)
            {
                string arraySuffix = null;
                do
                {
                    var rankCommas = new string(',', type.GetArrayRank() - 1);
                    type = type.GetElementType();
                    arraySuffix = arraySuffix + "[" + rankCommas + "]";
                }
                while (type.IsArray);
                var basename = CSharpTypeName(type);
                return basename + arraySuffix;
            }
            if (type.IsGenericType)
            {
                var typeArgs = type.GetGenericArguments();
                var typeArgIdx = typeArgs.Length;
                var revNestedTypeNames = new List<string>();

                while (type != null)
                {
                    var name = type.FullName;
                    var backtickIdx = name.IndexOf('`');
                    if (backtickIdx == -1)
                    {
                        revNestedTypeNames.Add(name);
                    }
                    else
                    {
                        var afterArgCountIdx = name.IndexOf('[', backtickIdx + 1);
                        if (afterArgCountIdx == -1)
                        {
                            afterArgCountIdx = name.Length;
                        }

                        var thisTypeArgCount = int.Parse(name.Substring(backtickIdx + 1, afterArgCountIdx - backtickIdx - 1));
                        var argNames = new List<string>();
                        for (var i = typeArgIdx - thisTypeArgCount; i < typeArgIdx; i++)
                        {
                            argNames.Add(CSharpTypeName(typeArgs[i]));
                        }

                        typeArgIdx -= thisTypeArgCount;
                        revNestedTypeNames.Add(name.Substring(0, backtickIdx) + "<" + string.Join(", ", argNames) + ">");
                    }

                    type = type.DeclaringType;
                }

                revNestedTypeNames.Reverse();
                return string.Join(".", revNestedTypeNames);
            }
            if (type.DeclaringType != null)
            {
                return CSharpTypeName(type.DeclaringType) + "." + type.Name;
            }
            return type.FullName;
        }    
    }
}