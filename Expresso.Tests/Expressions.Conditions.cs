namespace Expresso.Tests
{
    using System;
    using System.Linq.Expressions;
    using Expresso;
    using FluentAssertions;
    using Xunit;

    [Trait("Category", "Expresso - Conditions")]
    public class ExpressionsConditionsTests : ExpressionsTestBase
    {
        /// <summary>
        /// Парсинг выражения с условным результом
        /// </summary>
        /// <param name="expression">Выражение</param>
        /// <param name="input">Входящее значение</param>
        /// <param name="expect">Ожидаемое значение</param>
        [Theory(DisplayName = "Парсинг выражения с условным результом")]
        [InlineData("x => x * 2 > 3 ? x : 1", 1, 1)]
        [InlineData("x => x * 2 > 3 ? x : 1", 2, 2)]
        [InlineData("x => x + 2 - 1 <= 3 ? true : false", 1, true)]
        [InlineData("x => x + 2 - 1 <= 3 ? true : false", 5, false)]
        [InlineData("x => x + 2 - 1 >= 3", 2, true)]
        [InlineData("x => x + 2 - 1 > 3", 1, false)]
        [InlineData("x => x + 2 - 1 < 3", 1, true)]
        public void ShouldParseConditionalReturn(string expression, int input, object expect)
        {
            var result = ExpressionParser.FromString(expression).WithArgument<int>("x").Call<object>(input);

            result.Should().BeOfType(expect.GetType());
            result.Should().Be(expect);
        }

        [Fact(DisplayName = "Выражение с локальными переменнами и блоками if/else if/else")]
        public void ShouldParseIsThenElseCondition()
        {
            const string Expression = @"x => { 
                var i = 5; 
                if(x > 5) { 
                    var d = 10; 
                    return i + x + d;
                } 
                else if(x == 3) {
                    return 5
                } else {
                    return x;
                } 
            }";

            var builder = ExpressionParser
                .FromString(Expression)
                .WithArgument<int>("x");

            LambdaExpression expr = null;
            Action action = () => expr = builder.ToLambda<int>();

            action.ShouldNotThrow();
            expr.Should().NotBeNull();

            var fn = (Func<int, int>)expr.Compile();

            fn.Invoke(10).ShouldBeEquivalentTo(25);
            fn.Invoke(3).ShouldBeEquivalentTo(5);
            fn.Invoke(1).ShouldBeEquivalentTo(1);
        }

        [Fact(DisplayName = "Switch-выражение с return и break")]
        public void ShouldParseSwitchExpression()
        {
            const string Expression = @"x => {
                switch(x){
                    case 1:
                    case 2: 
                        return x == 1 ? 0 : 1;
                    case 3: 
                        break;
                    case 4:
                        return 1;
                    default:
                        break;
                }
                return 10;
            }";

            var builder = ExpressionParser
                .FromString(Expression)
                .WithArgument<int>("x");

            LambdaExpression expr = null;
            Action action = () => expr = builder.ToLambda<int>();

            action.ShouldNotThrow();
            expr.Should().NotBeNull();

            var fn = (Func<int, int>)expr.Compile();

            fn.Invoke(1).ShouldBeEquivalentTo(0);
            fn.Invoke(2).ShouldBeEquivalentTo(1);
            fn.Invoke(3).ShouldBeEquivalentTo(10);
            fn.Invoke(4).ShouldBeEquivalentTo(1);
            fn.Invoke(5).ShouldBeEquivalentTo(10);
        }
    }
}