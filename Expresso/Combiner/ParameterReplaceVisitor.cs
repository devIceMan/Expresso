namespace Expresso.Combiner
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    /// <summary>
    /// Обходчик, по имени заменяющий параметры в выражении на заданные в конструкторе 
    /// </summary>
    internal class ParameterReplaceVisitor
        : ExpressionVisitor
    {
        /// <summary>
        /// Контейнер новых параметров для замены
        /// </summary>
        private readonly Dictionary<string, ParameterExpression> _newValues;

        /// <summary>
        /// Конструктор обходчика, по имени заменяющий параметры в выражении на заданные в конструкторе
        /// </summary>
        /// <param name="newValue">Параметр для замены</param>
        public ParameterReplaceVisitor(IEnumerable<ParameterExpression> newValue)
        {
            _newValues = newValue.ToDictionary(x => x.Name);
        }

        /// <summary>
        /// Если находится параметр по имени, который содержится в контейнере, то заменяется
        /// на соответствующий из контейнера
        /// </summary>
        /// <param name="node">Параметр</param>
        /// <returns>Выражение</returns>
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return _newValues.ContainsKey(node.Name)
                       ? _newValues[node.Name]
                       : base.VisitParameter(node);
        }
    }
}