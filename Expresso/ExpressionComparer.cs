namespace Expresso
{
    using System.Collections.Generic;
    using System.Linq.Expressions;

    internal class ExpressionComparer : IEqualityComparer<Expression>
    {
        static ExpressionComparer()
        {
            Instance = new ExpressionComparer();
        }

        public static ExpressionComparer Instance { get; private set; }

        /// <summary>Determines whether the specified objects are equal.</summary>
        /// <param name="x">The first object of type to compare.</param>
        /// <param name="y">The second object of type to compare.</param>
        /// <returns>True if the specified objects are equal; otherwise, false.</returns>
        public bool Equals(Expression x, Expression y)
        {
            // both nulls? comparison is OK
            if (x == null && y == null)
            {
                return true;
            }

            if (x == null)
            {
                return false;
            }

            if (y == null)
            {
                return false;
            }

            // first thing: check types
            if (x.GetType() != y.GetType())
            {
                return false;
            }

            // then check node types
            if (x.NodeType != y.NodeType)
            {
                return false;
            }

            // finally, check expression types
            if (x.Type != y.Type)
            {
                return false;
            }

            return x.ToString() == y.ToString();
        }

        /// <summary>Returns a hash code for the specified object.</summary>
        /// <param name="obj">The <see cref="T:System.Object"/> for which a hash code is to be returned.</param>
        /// <returns>A hash code for the specified object.</returns>
        /// <exception cref="T:System.ArgumentNullException">The type of <paramref name="obj"/> is a reference type and <paramref name="obj"/> is null.</exception>
        public int GetHashCode(Expression obj)
        {
            return (int)obj.NodeType ^ obj.GetType().GetHashCode();
        }
    }
}