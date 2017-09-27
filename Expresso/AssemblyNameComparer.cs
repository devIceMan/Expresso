namespace Expresso
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// Механизм сравнения <see cref="Assembly"/> по простому имени
    /// </summary>
    internal class AssemblyNameComparer : IEqualityComparer<Assembly>
    {
        public static readonly AssemblyNameComparer Instance = new AssemblyNameComparer();

        public bool Equals(Assembly x, Assembly y)
        {
            var nameX = x?.GetName()?.Name;
            var nameY = y?.GetName()?.Name;

            if (nameX == null || nameY == null)
                return false;

            return StringComparer.Ordinal.Equals(nameX, nameY);
        }

        public int GetHashCode(Assembly obj)
        {
            return obj.GetName().Name.GetHashCode();
        }
    }
}