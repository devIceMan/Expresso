namespace Expresso.Tests.Data
{
    using System.Collections.Generic;

    public class TestEntity 
    {
        /// <summary>
        /// Общая сумма
        /// </summary>
        public int TotalSum { get; set; }

        /// <summary>
        /// Подсумма
        /// </summary>
        public int SubSum { get; set; }

        /// <summary>
        /// Значение
        /// </summary>
        public long? LongValue { get; set; }

        /// <summary>
        /// Коллекция вложенных элементов TestItem
        /// </summary>
        public ICollection<TestItem> Nested { get; set; }
    }
}
