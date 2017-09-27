namespace Expresso
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;

    /// <summary>
    /// Обертка над BlockExpression, используемый для объявления переменных.
    /// Нужен для определения необходимости передачи переменных в родительский блок
    /// </summary>
    internal class VariableBlockWrapper : Expression
    {
        private readonly BlockExpression _inline;

        public VariableBlockWrapper(BlockExpression inline)
        {
            this._inline = inline;
        }

        public IEnumerable<Expression> Expressions => _inline.Expressions;

        public IEnumerable<ParameterExpression> Variables => _inline.Variables;
    }

    internal class LabelMarker
    {
        public string Name { get; set; }

        public Type Type { get; set; }

        public LabelTarget Target { get; set; }
    }
}