namespace Expresso.Tests.Data
{
    /// <summary>
    /// Вложенный элемент
    /// </summary>
    public class TestSubItem 
    {        
        /// <summary>
        /// Цена
        /// </summary>
        public int Cost { get; set; }

        /// <summary>
        /// Количество
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Идентификатор родительского элемента
        /// </summary>
        public int ItemId { get; set; }

        /// <summary>
        /// Экземпляр родительского элемента TestItem
        /// </summary>        
        public TestItem Item { get; set; }
    }    
}