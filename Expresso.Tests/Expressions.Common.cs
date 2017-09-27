namespace Expresso.Tests
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq.Expressions;
    using Expresso;
    using Expresso.Extensions;
    using Expresso.Tests.Data;
    using FluentAssertions;
    using Xunit;

    [Trait("Category", "Expresso - Common")]
    public class ExpressionsCommonTests : ExpressionsTestBase
    {
        /// <summary>
        /// Использование псевдонимов типов
        /// </summary>
        [Fact(DisplayName = "Использование псевдонимов типов")]
        public void TypeAliasTests()
        {
            const string Expression = "()=>{ var x = new ListOfInt(); return x; }";
            Func<List<int>> expr = null;
            Action action = () => expr = ExpressionParser.FromString(Expression)
                .UsingNamespaceOf(typeof(List<>))
                                             .UsingTypeAlias<List<int>>("ListOfInt")
                                             .ToFunction<List<int>>();

            action.ShouldNotThrow();
            expr.Should().NotBeNull();

            var list = expr.Invoke();
            list.Should().BeAssignableTo<List<int>>();
        }

        [Theory(DisplayName = "Парсинг простых выражений с разными типами аргументов")]
        [InlineData("(x,y)=>x + y", "10", 1, "101")]
        [InlineData("(x,y)=>x", "20", 2, "20")]
        [InlineData("(x,y)=>int.Parse(x)", "30", 3, 30)]
        [InlineData("(x,y)=>int.Parse(x) + y", "30", 3, 33)]
        public void ParseLambda(string expr, object arg1, object arg2, object result)
        {
            var builder = ExpressionParser.FromString(expr).WithArgument<string>("x").WithArgument<long>("y");
            
            var r = builder.Call(result.GetType(), arg1, arg2);
            r.Should().NotBeNull();
            r.Should().BeOfType(result.GetType());

            r.Should().Be(result);
        }
        
        /// <summary>
        /// Передача параметра в локальную переменную и условный вывод
        /// </summary>
        /// <param name="a">Параметр 1</param>
        /// <param name="b">Параметр 2</param>
        /// <param name="expect">Ожидаемое значение</param>
        [Theory(DisplayName = "Передача параметра в локальную переменную и условный вывод")]
        [InlineData(1, 2, false)]
        [InlineData(2, 1, true)]
        public void VariableTest(int a, int b, bool expect)
        {
            var expr = "(a,b)=>{ var x = a; int y = b; return x > y ? true : false; };";
            var builder = ExpressionParser.FromString(expr).WithArgument<int>("a").WithArgument<int>("b");

            var result = false;
            Action action = () => result = builder.Call<bool>(a, b);
            action.ShouldNotThrow();

            result.Should().Be(expect);
        }

        /// <summary>
        /// Использование неименованных параметров
        /// </summary>
        [Fact(DisplayName = "Использование неименованных параметров")]
        public void UnnamedParametersTest()
        {
            var builder = ExpressionParser.FromString("x => string.Format(\"{0}\", x)").WithArgument<object>();

            var expr = builder.Call<string>(12);

            expr.Should().BeEquivalentTo("12");
        }

        /// <summary>
        /// Построение функций
        /// </summary>
        [Fact(DisplayName = "Построение функций")]
        public void FunctionTests()
        {
            var builder = ExpressionParser.FromString("(x,y) => string.Format(\"{0}-{1}\", x,y)").WithArgument<object>().WithArgument<int>();

            var expr = builder.ToParamsFunction<string>();

            var r = expr.Invoke("12", 1);

            r.Should().BeEquivalentTo("12-1");
        }

        [Theory(DisplayName = "Создание экземпляров generic-типов")]
        [InlineData("()=>new System.Collections.Generic.List<string>()", typeof(List<string>))]
        [InlineData("()=>new System.Tuple<string>(\"string\")", typeof(Tuple<string>))]
        [InlineData("()=>new System.Tuple<string, int>(\"string\", 0)", typeof(Tuple<string, int>))]
        [InlineData("()=>new System.Collections.Generic.Dictionary<string, int>()", typeof(Dictionary<string, int>))]
        public void GenericsTests(string expression, Type expectedType)
        {
            var expr = ExpressionParser.FromString(expression);
            var result = expr.Call<object>();

            result.Should().BeOfType(expectedType);
        }

        [Fact(DisplayName = "Разбор выражения с локальными переменными внутри блока")]
        public void ShouldParseExpressionWithVariablesInBlock()
        {
            const string Expression = "x => { var i = 5; return i + x; }";
            var builder = ExpressionParser.FromString(Expression).WithArgument<int>("x");
            LambdaExpression expr = null;
            Action action = () => expr = builder.ToLambda<int>();

            action.ShouldNotThrow();
            expr.Should().NotBeNull();

            Func<int, int> fn = null;
            action = () => fn = builder.ToFunction<int, int>();

            action.ShouldNotThrow();

            fn.Invoke(5).ShouldBeEquivalentTo(10);
        }

        [Fact(DisplayName = "Разбор выражения с выбросом исключения")]
        public void ShouldParseAndThrowParsedException()
        {
            const string Expression = "() => { throw new Exception(\"Ошибка обработки\") }";
            var builder = ExpressionParser.FromString(Expression).UsingNamespaceOf<Exception>();

            LambdaExpression expr = null;
            Action action = () => expr = builder.ToLambda(typeof(void));

            action.ShouldNotThrow();

            action = (Action)expr.Compile();
            action.ShouldThrow<Exception>();
        }

        [Fact(DisplayName = "Разбор выражения с as")]
        public void ShouldParseTypeAsExpression()
        {
            const string Expression = @"() => { 
                                            var typeValue = typeof(string);
                                            typeValue = typeof(System.String);
                                            return typeValue as System.Type;
                                        }";

            var builder = ExpressionParser.FromString(Expression).Using<Type>();
            LambdaExpression expr = null;
            Action action = () => expr = builder.ToLambda<Type>();

            action.ShouldNotThrow();
            Type type = null;

            action = () => {
                var @delegate = expr.Compile();
                type = @delegate.DynamicInvoke() as Type;
            };

            action.ShouldNotThrow();
            type.Should().NotBeNull();            
            type.FullName.Should().Be("System.String");
        }

        [Fact(DisplayName = "Разбор выражения с is")]
        public void ShouldParseTypeIsExpression()
        {
            const string Expression = @"() => { 
                                            var stringValue = ""Value"";                                            
                                            return stringValue is string;
                                        }";

            var builder = ExpressionParser.FromString(Expression).Using<Type>().Using<string>();
            Func<bool> expr = null;
            Action action = () => expr = builder.ToFunction<bool>();

            action.ShouldNotThrow();
            var valueIsString = false;

            action = () => {
                valueIsString = expr.Invoke();                
            };

            action.ShouldNotThrow();
            valueIsString.Should().BeTrue();            
        }

        [Fact(DisplayName = "Разбор выражения с приведением типа")]
        public void ShouldParseTypeConvertExpression()
        {
            const string Expression = @"() => { 
                                            object stringValue = ""Value"";                                            
                                            return (string)stringValue;
                                        }";

            var builder = ExpressionParser.FromString(Expression).Using<Type>().Using<string>();
            Func<string> expr = null;
            Action action = () => expr = builder.ToFunction<string>();

            action.ShouldNotThrow();
            var stringValue = "";

            action = () => {
                stringValue = expr.Invoke();
            };

            action.ShouldNotThrow();
            stringValue.Should().Be("Value");
        }

        [Fact(DisplayName = "Конвертация аргумента вызова")]
        public void ShouldParseAndConvertArgument()
        {
            const string Expression = @"()=> ExpressionsCommonTests.LongParamInput(1);";

            var builder = ExpressionParser
                .FromString(Expression)
                .Using<Type>()
                .Using<string>()
                .Using<ExpressionsCommonTests>();

            Expression ex = builder.ToLambda<long>();

            Func<long> expr = null;
            Action action = () => expr = builder.ToFunction<long>();

            action.ShouldNotThrow();

            long value = 0;
            action = () => value = expr();

            action.ShouldNotThrow();
            value.Should().Be(1);
        }

        [Fact(DisplayName = "Конвертация аргумента вызова методы расширения")]
        public void ShouldParseAndConvertArgumentOfExtensionMethod()
        {
            const string Expression = @"()=> 1.LongArg(1);";

            var builder = ExpressionParser
                .FromString(Expression)
                .Using<Type>()
                .Using<string>()
                .Using(typeof(TestExtensions));

            Expression ex = builder.ToLambda<long>();

            Func<long> expr = null;
            Action action = () => expr = builder.ToFunction<long>();

            action.ShouldNotThrow();

            long value = 0;
            action = () => value = expr();

            action.ShouldNotThrow();
            value.Should().Be(2);
        }

        public static long IntParamToLong(int value) => (long)value;

        public static long LongParamInput(long value) => value;
    }

    public static class TestExtensions
    {
        public static long LongArg(this int value, long arg) => arg + value;
    }
}