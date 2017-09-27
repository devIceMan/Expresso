namespace Expresso.ExpressionToCode
{
    using System;
    using System.Linq.Expressions;

    internal class EnumProcessor : ConstantExpressionProcessor
    {
        public EnumProcessor(ExpressionWalker walker)
            : base(walker) { }

        public override bool CanProcess(ConstantExpression expression, object value)
        {
            return value != null && value.GetType().IsEnum;
        }

        public override string Process(ConstantExpression expression, object value)
        {
            var type = value.GetType();
            var name = Enum.GetName(type, value);
            return $"{type.FullName}.{name}";
        }
    }
}