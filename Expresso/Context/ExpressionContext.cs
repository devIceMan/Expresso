namespace Expresso.Context
{
    using System.Collections.Generic;

    /// <summary>
    /// Хранилище аргументов выражения.
    /// Рекомендуется применять для автоматически сгенерированных выражений.
    /// </summary>
    public class ExpressionContext : IExpressionContext
    {
        private readonly IDictionary<string, object> _bag = new Dictionary<string, object>();

        /// <summary>
        /// Установить значение аргумента
        /// </summary>
        /// <typeparam name="T"> Тип аргумента </typeparam>
        /// <param name="key"> Ключ аргумента </param>
        /// <param name="value"> Значение аргумента </param>
        /// <returns> </returns>
        public IExpressionContext Set<T>(string key, T value)
        {
            _bag[key] = value;
            return this;
        }

        /// <summary>
        /// Получить значение аргумента
        /// </summary>
        /// <typeparam name="T"> Тип аргумента </typeparam>
        /// <param name="key"> Ключ аргумента </param>
        /// <returns> </returns>
        public T Get<T>(string key)
        {
            object value;
            if (!_bag.TryGetValue(key, out value) || value == null)
            {
                value = default(T);
            }

            return (T) value;
        }
    }
}