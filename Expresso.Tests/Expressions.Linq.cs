namespace Expresso.Tests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Expresso;
    using Expresso.Tests.Data;
    using FluentAssertions;
    using Xunit;

    [Trait("Category", "Expresso - Linq")]
    public class ExpressionsLinqTests : ExpressionsTestBase
    {
        //[Theory(DisplayName = "C# -> Expression -> С#")]
        //[InlineData("obj => obj.Nested.Sum(x => x.Item)")]
        //[InlineData("obj => obj.Nested.Sum(x => x.Item + x.Nested.Sum(y => y.Cost * y.Count))")]
        //[InlineData("obj => obj.Nested.First(x => x.Item > 10).Nested.Sum(y => y.Cost * y.Count)")]
        //[InlineData("obj => obj.Nested.First(x => x.Item == 10 && (x.Item == 20 || x.Item == 20))")]
        //public void OriginalStringAndParsedExpressionShoudBeTheSame(string csharpExpression)
        //{
        //    var expression = ExpressionParser.FromString(csharpExpression).WithArgument<TestEntity>("obj").ToLambda<object>();
        //    var text = ExpressionParser.ToString(expression);
        //    text.Should().Be(csharpExpression);
        //}

        //[Theory(DisplayName = "Парсинг LINQ-запросов")]
        //[InlineData("s.GetAll().Select(x=>new Expresso.UnitTests.TestDto { x.Id, TotalSum = x.TotalSum * 2 })")]
        //[InlineData("(s) => s.GetAll().Select(x=>new Expresso.UnitTests.TestDto { x.Id, TotalSum = x.TotalSum * 2 })")]
        //[InlineData("s => s.GetAll().Select(x=>new Expresso.UnitTests.TestDto { x.Id, TotalSum = x.TotalSum * 2 })")]
        //public void ParseQuery(string query)
        //{
        //    var ds = new TestDomainService();
        //    var builder = ExpressionParser
        //        .FromString(query)
        //        .WithAssemblyOf<TestEntity>()
        //        .WithArgument<TestDomainService>("s");

        //    IEnumerable enumerable = null;
        //    Action action = () => enumerable = builder.Call<IEnumerable>(ds);

        //    action.ShouldNotThrow();
        //    enumerable.Should().NotBeNull();
        //    enumerable.Should().HaveCount(100);
        //}

        ///// <summary>
        ///// Парсинг linq-запроса с анонимным типом
        ///// </summary>
        //[Fact(DisplayName = "Парсинг LINQ-запроса с анонимным типом")]
        //public void AnonymousTypeTest()
        //{
        //    const string Expression = "p => p.GetAll().Select(x=>new { x.Id, TotalSumX2 = x.TotalSum * 2, TotalSum = x.TotalSum + 1, x.Id + x.TotalSum })";

        //    var builder = ExpressionParser
        //        .FromString(Expression)
        //        .WithAssemblyOf<TestEntity>()
        //        .WithArgument<TestDomainService>("p");

        //    var r = builder.Call<IEnumerable>(DomainSetvice);
        //    r.Should().NotBeNull();
        //}

        ///// <summary>
        ///// Парсинг linq-запроса с массивом анонимных типов и сравнение экземпляров
        ///// </summary>
        //[Fact(DisplayName = "Парсинг LINQ-запроса с массивом анонимных типов и сравнение экземпляров результата")]
        //public void AnonymousTypeTestWithEquality()
        //{
        //    const string Expression = "p => p.GetAll().Select(x=> new[]{ new { x.Id, TotalSum = x.TotalSum }, new { x.Id, TotalSum = x.TotalSum } })";

        //    var builder = ExpressionParser
        //        .FromString(Expression)
        //        .WithAssemblyOf<TestEntity>()
        //        .WithArgument<TestDomainService>("p");

        //    var r = builder.Call<IEnumerable>(DomainSetvice);
        //    r.Should().NotBeNull();
        //    r.Should().HaveCount(100);

        //    var pair = r.Cast<object[]>().First();
        //    pair[0].ShouldBeEquivalentTo(pair[1], x => x.IncludingAllDeclaredProperties().IncludingAllRuntimeProperties().IncludingFields());
        //}

        //[Theory(DisplayName = "Вызов методов расширения LINQ")]
        //[InlineData("list => { var x = new System.Collections.Generic.List<int>() { -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5 }; return x.Where(v => v > 0)}")]
        //[InlineData("x => x.Where(v=>v > 3).Select(v => v)")]
        //[InlineData("x => x.Where(v=>v > 3).Select(v => v).Cast<int>()")]
        //public void LinqExtensionsTests(string expression)
        //{
        //    var data = new List<int> { -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5 };

        //    var expr = ExpressionParser.FromString(expression).WithAssemblyOf<TestEntity>().WithArgument<IQueryable<int>>();

        //    object result = null;
        //    Action action = () => result = expr.Call<object>(data.AsQueryable());
        //    action.ShouldNotThrow();
        //    result.Should().NotBeNull();
        //}
    }
}