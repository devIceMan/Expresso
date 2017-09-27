namespace Expresso.Context
{
    /// <summary>
    /// Интерфейс хранилища аргументов выражения.
    /// Применяется для сокращения выражения, чтобы вместо
    /// <code>(a, b, c) => ...</code>
    /// можно было писать
    /// <code>ctx => ctx.Get(x)</code>
    /// Рекомендуется применять для автоматически сгенерированных выражений.
    /// </summary>
    public interface IExpressionContext
    {
        /// <summary>
        /// Установить значение аргумента
        /// </summary>
        /// <typeparam name="T"> Тип аргумента </typeparam>
        /// <param name="key"> Ключ аргумента </param>
        /// <param name="value"> Значение аргумента </param>
        /// <returns> </returns>
        IExpressionContext Set<T>(string key, T value);

        /// <summary>
        /// Получить значение аргумента
        /// </summary>
        /// <typeparam name="T"> Тип аргумента </typeparam>
        /// <param name="key"> Ключ аргумента </param>
        /// <returns> </returns>
        T Get<T>(string key);
    }
}