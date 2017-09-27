namespace Expresso.ExpressionToCode {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Expresso.Extensions;

    internal class SimpleStructProcessor : ConstantExpressionProcessor
    {
        public SimpleStructProcessor(ExpressionWalker walker)
            : base(walker) { }

        public override bool CanProcess(ConstantExpression expression, object value)
        {
            var type = value?.GetType();
            if (type?.IsValueType != true)
                return false;

            return !type.IsPrimitive && !type.IsEnum && type.Namespace?.StartsWith("System") != true;
        }

        public override string Process(ConstantExpression expression, object value)
        {
            var type = value.GetType();
            var ctor = type.GetConstructors().FirstOrDefault();
            var properties = type.GetProperties().Where(x => x.CanRead && x.CanWrite && x.GetIndexParameters().IsEmpty()).ToArray();
            var initializer = new List<string>();

            if (ctor == null)
            {                
                foreach (var property in properties)
                {
                    var propertyValue = Walker.Visit(Expression.Constant(property.GetValue(value)));
                    initializer.Add($"{property.Name} = {propertyValue.Key}");
                }

                return $"new {type.FullName} {{ {string.Join(", ", initializer)} }}";
            }

            foreach (var parameter in ctor.GetParameters())
            {
                var property = properties.First(x => x.Name.Equals(parameter.Name, StringComparison.OrdinalIgnoreCase));
                var propertyValue = Walker.Visit(Expression.Constant(property.GetValue(value)));
                initializer.Add($"{propertyValue.Key}");
            }

            return $"new {type.FullName}({string.Join(", ", initializer)})";            
        }
    }
}