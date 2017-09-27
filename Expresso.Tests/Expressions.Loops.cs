namespace Expresso.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Text;
    using Expresso;
    using Expresso.Tests.Data;
    using FluentAssertions;
    using Xunit;

    [Trait("Category", "Expresso - Loops")]
    public class ExpressionsLoopsTests : ExpressionsTestBase
    {
        [Fact(DisplayName = "For-цикл с передачей переменной цикла во внутренний блок")]
        public void ShouldParseForLoopWithBlockInside()
        {
            const string Expression = @"(x, sb) => { 
                var result = 0;
                for(var i = 0; i <= x; i++){
                    result++;
                    if (i % 2 != 0) {
                        sb.Append(i.ToString());
                    }
                }

                return result;
            }";

            var builder = ExpressionParser
                .FromString(Expression)
                .WithArgument<int>("x")
                .WithArgument<StringBuilder>("sb");

            LambdaExpression expr = null;
            Action action = () => expr = builder.ToLambda<int>();

            action.ShouldNotThrow();
            expr.Should().NotBeNull();

            var sb = new StringBuilder();
            var fn = (Func<int, StringBuilder, int>)expr.Compile();
            fn.Invoke(10, sb).ShouldBeEquivalentTo(11);
            sb.ToString().ShouldBeEquivalentTo("13579");
        }

        //[Fact(DisplayName = "Foreach-цикл с возвратом списка объектов")]
        //public void ShouldParseForEachLoop()
        //{
        //    const string Expression = @"ds => {
        //        var list = ds.GetAll();
        //        var result = new List<object>();
        //        foreach(var x in list){
        //            result.Add(x);                    
        //        }

        //        return result;
        //    }";

        //    var builder = ExpressionParser
        //        .FromString(Expression)
        //        .WithArgument<TestDomainService>("ds")
        //        .UsingNamespaceOf<List<object>>();

        //    LambdaExpression expr = null;
        //    Action action = () => expr = builder.ToLambda<List<object>>();

        //    action.ShouldNotThrow();
        //    expr.Should().NotBeNull();

        //    var fn = (Func<TestDomainService, List<object>>)expr.Compile();
        //    fn.Invoke(DomainSetvice).Should().HaveCount(100);
        //}

        [Fact(DisplayName = "Foreach-цикл с использованием continue и break")]
        public void ShouldParseForEachLoopWithContinueAndBreak()
        {
            const string Expression = @"() => {
                var list = new [] { 1, 2, 3, 4, 5 };
                var result = 0;
                foreach(var x in list){
                    if (x < 4) continue;
                    else {
                        result = x;
                        break;
                    }
                }

                return result;
            }";

            var builder = ExpressionParser
                .FromString(Expression);

            LambdaExpression expr = null;
            Action action = () => expr = builder.ToLambda<int>();

            action.ShouldNotThrow();
            expr.Should().NotBeNull();

            var fn = (Func<int>)expr.Compile();
            fn.Invoke().ShouldBeEquivalentTo(4);
        }
    }
}