using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Expresso.Extensions
{
    using System.Linq.Expressions;

    public static class StringExtensions
    {
        public static bool IsEmpty(this string @string) => string.IsNullOrWhiteSpace(@string);

        public static bool IsNotEmpty(this string @string) => !@string.IsEmpty();
    }

    public static class EnumerableExtensions
    {
        public static bool IsEmpty<T>(this IEnumerable<T> enumerable) => enumerable == null || !enumerable.Any();

        public static bool IsNotEmpty<T>(this IEnumerable<T> enumerable) => !enumerable.IsEmpty();

        public static bool IsEmpty<T>(this T[] array) => array == null || array.Length == 0;

        public static bool IsNotEmpty<T>(this T[] array) => !array.IsEmpty();

        public static bool IsEmpty<T>(this ICollection<T> collection) => collection == null || collection.Count == 0;

        public static bool IsNotEmpty<T>(this ICollection<T> collection) => !collection.IsEmpty();
    }

    public static class DictionaryExtensions
    {
        public static T GetOrCreate<T>(this IDictionary<string, object> dictionary, string key, Func<T> valueProvider)
        {
            object value;
            if (!dictionary.TryGetValue(key, out value))
            {
                value = valueProvider.Invoke();
                dictionary[key] = value;
            }

            return (T) value;
        }
    }
}
