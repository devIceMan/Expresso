namespace Expresso.ExpressionToCode
{
    using System.Linq.Expressions;

    /// <summary>
    /// ����������� ����� ������� ��������� ��������
    /// </summary>
    internal abstract class ConstantExpressionProcessor
    {
        protected ConstantExpressionProcessor(ExpressionWalker walker)
        {
            Walker = walker;
        }

        protected ExpressionWalker Walker { get; }

        /// <summary>
        /// �������� ����������� ��������� ���������
        /// </summary>
        /// <param name="expression"> </param>
        /// <param name="value"> </param>
        /// <returns> </returns>
        public abstract bool CanProcess(ConstantExpression expression, object value);

        /// <summary>
        /// ������ ��������� � ������������ ���������� �������������
        /// </summary>
        /// <param name="expression"> </param>
        /// <param name="value"> </param>
        /// <returns> </returns>
        public abstract string Process(ConstantExpression expression, object value);
    }

    internal abstract class ConstantExpressionProcessor<T> : ConstantExpressionProcessor
    {
        protected ConstantExpressionProcessor(ExpressionWalker walker)
            : base(walker) { }

        /// <inheritdoc />
        public override bool CanProcess(ConstantExpression expression, object value)
        {
            return expression != null && value != null && value.GetType() == typeof(T);
        }

        /// <inheritdoc />
        public override string Process(ConstantExpression expression, object value)
        {
            return ProcessTypedValue(expression, (T) value);
        }

        /// <summary>
        /// ��������� ��������������� ��������
        /// </summary>
        /// <param name="expression"> </param>
        /// <param name="value"> </param>
        /// <returns> </returns>
        protected abstract string ProcessTypedValue(ConstantExpression expression, T value);
    }
}