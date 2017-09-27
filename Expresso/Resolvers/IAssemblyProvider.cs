namespace Expresso.Resolvers
{
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// Интерфейс поставщика сборок
    /// </summary>
    public interface IAssemblyProvider
    {
        /// <summary>
        /// Возвращает набор сборок
        /// </summary>        
        IEnumerable<Assembly> GetAssemblies();
    }
}