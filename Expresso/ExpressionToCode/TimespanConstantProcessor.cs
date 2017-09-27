namespace Expresso.ExpressionToCode
{
    using System;
    using System.Linq.Expressions;

    internal class TimespanConstantProcessor : ConstantExpressionProcessor<TimeSpan>
    {
        public TimespanConstantProcessor(ExpressionWalker walker)
            : base(walker) { }

        protected override string ProcessTypedValue(ConstantExpression expression, TimeSpan value)
        {
            return $"TimeSpan.FromTicks({value.Ticks})";
        }
    }
}