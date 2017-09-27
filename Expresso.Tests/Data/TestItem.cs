namespace Expresso.Tests.Data
{
    using System.Collections.Generic;

    /// <summary>
    /// Тестовый элемент
    /// </summary>
    public class TestItem 
    {    
        /// <summary>
        /// Элемент
        /// </summary>
        public int Item { get; set; }

        /// <summary>
        /// Сумма
        /// </summary>
        public int ItemSum { get; set; }

        /// <summary>
        /// Идентификатор контекста
        /// </summary>
        public int ContextId { get; set; }

        /// <summary>
        /// Экземпляр контекста
        /// </summary>        
        public TestItem Parent { get; set; }

        /// <summary>
        /// Коллекция вложенных элементов TestSubItem
        /// </summary>
        public ICollection<TestSubItem> Nested { get; set; }
    }
}