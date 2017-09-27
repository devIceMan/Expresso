namespace Expresso.ExpressionToCode {
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq.Expressions;

    //using Castle.Components.DictionaryAdapter.Xml;

    internal class SimpleGenericListProcessor : ConstantExpressionProcessor
    {
        public SimpleGenericListProcessor(ExpressionWalker walker)
            : base(walker) { }

        public override bool CanProcess(ConstantExpression expression, object value)
        {
            var type = value.GetType();
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
        }

        public override string Process(ConstantExpression expression, object value)
        {
            var type = value.GetType();
            var itemType = type.GetGenericArguments()[0];
            var items = new List<string>();
            var enumerable = (IEnumerable) value;
            var enumerator = enumerable.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var listItem = Walker.Visit(Expression.Constant(enumerator.Current));
                items.Add(listItem.Key);
            }
            
            return $"new System.Collections.Generic.List<{itemType.FullName}> {{ {string.Join(", ", items)} }}";
        }
    }
}