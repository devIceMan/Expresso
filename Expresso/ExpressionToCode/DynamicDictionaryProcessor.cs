namespace Expresso.ExpressionToCode
{
    using System.Linq.Expressions;
    using System.Text;

    //internal class DynamicDictionaryProcessor : ConstantExpressionProcessor<DynamicDictionary>
    //{
    //    public DynamicDictionaryProcessor(ExpressionWalker walker)
    //        : base(walker) { }

    //    protected override string ProcessTypedValue(ConstantExpression expression, DynamicDictionary value)
    //    {
    //        var sb = new StringBuilder().Append("DynamicDictionary.Create()");
    //        foreach (var pair in value.OrderBy(x => x.Key))
    //        {
    //            var dictionaryValue = Walker.Visit(Expression.Constant(pair.Value));
    //            sb.Append($".SetValue(\"{pair.Key}\", {dictionaryValue.Key})");
    //        }

    //        return sb.ToString();
    //    }
    //}
}