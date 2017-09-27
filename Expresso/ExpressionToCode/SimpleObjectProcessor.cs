namespace Expresso.ExpressionToCode {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Expresso.Extensions;

    internal class SimpleObjectProcessor : ConstantExpressionProcessor
    {
        public SimpleObjectProcessor(ExpressionWalker walker)
            : base(walker) { }

        public override bool CanProcess(ConstantExpression expression, object value)
        {
            if (value == null)
                return false;

            var type = value.GetType();
            if (!type.IsClass || type.IsInterface || type.IsAbstract)
                return false;

            var defaultCtor = type.GetConstructor(new Type[0]);
            return defaultCtor != null;
        }

        public override string Process(ConstantExpression expression, object value)
        {
            var type = value.GetType();
            var properties = type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.CanRead && x.CanWrite && x.GetIndexParameters().IsEmpty())
                .OrderBy(x => x.Name)
                .ToArray();

            var initializers = new List<string>();
            foreach (var property in properties)
            {
                var propertyValue = property.GetValue(value);
                if (propertyValue == null)
                    continue;

                var expressionValue = Walker.Visit(Expression.Constant(propertyValue));
                initializers.Add($"{property.Name} = {expressionValue.Key}");
            }

            return $"new {type.FullName} {{ {string.Join(", ", initializers)} }}";
        }
    }
}