using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Expresso.Utils
{
    using Expresso.Extensions;

    public static class ArgumentChecker
    {
        public static void NotNull(object value, string nameOfParameter)
        {
            if (value == null)
                throw new ArgumentNullException(nameOfParameter);
        }

        public static void NotEmpty(string value, string nameOfParameter)
        {
            if (value.IsEmpty())
                throw new ArgumentException("Input string is null or empty", nameOfParameter);
        }

        public static void NotNullAndIs<T>(object value, string nameOfParameter)
        {
            NotNull(value, nameOfParameter);

            var type = value as Type;
            if (type != null && typeof(T).IsAssignableFrom(type))
                throw new ArgumentException($"Input parameter is not assignable to {typeof(T).FullName}", nameOfParameter);

            if (!(value is T))
                throw new ArgumentException($"Input parameter is not assignable to {typeof(T).FullName}", nameOfParameter);
        }
    }
}
