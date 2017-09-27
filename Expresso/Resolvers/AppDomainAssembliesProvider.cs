namespace Expresso.Resolvers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Поставщик сборок текущего домена
    /// </summary>
    public class AppDomainAssembliesProvider : IAssemblyProvider
    {
        /// <summary>
        /// Возвращает набор сборок
        /// </summary>        
        public IEnumerable<Assembly> GetAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic);
        }
    }
}