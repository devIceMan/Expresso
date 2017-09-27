namespace Expresso.Tests
{
    using System;
    using Expresso;
    using Expresso.Context;
    using FluentAssertions;
    using Xunit;

    [Trait("Category", "Expresso - Delegate")]
    public class ExpressionsDelegatesTests : ExpressionsTestBase
    {
        [Fact(DisplayName = "Парсинг и выполнение Predicate<DateTime?> с использование контекста")]
        public void DateTimeIsEmptyPredicateCtxParametersParserTest()
        {
            var expression = "(new Predicate<DateTime?>(x_=>!x_.HasValue||x_.Value.Date==DateTime.MinValue.Date).Invoke(ctx.Get<System.DateTime>(\"Срок, к которому нужна техника\")))";

            var result = false;
            Func<IExpressionContext, bool> fn = null;
            Action action = () => fn = ExpressionParser
                                           .FromString(expression)
                                           .WithArgument<IExpressionContext>("ctx")
                                           .ToFunction<IExpressionContext, bool>();

            action.ShouldNotThrow();
            fn.Should().NotBeNull();

            var helper = new ExpressionContext();

            action = () => result = fn.Invoke(helper);

            helper.Set("Срок, к которому нужна техника", DateTime.Now);
            action.ShouldNotThrow();
            result.Should().BeFalse();

            helper.Set("Срок, к которому нужна техника", DateTime.MinValue);
            action.ShouldNotThrow();
            result.Should().BeTrue();

            helper.Set("Срок, к которому нужна техника", DateTime.MinValue.AddHours(12));
            action.ShouldNotThrow();
            result.Should().BeTrue();

            helper.Set<object>("Срок, к которому нужна техника", null);
            action.ShouldNotThrow();
            result.Should().BeTrue();
        }

        /// <summary>
        /// Парсинг и выполнение Func(DateTime?, bool)
        /// </summary>
        [Fact(DisplayName = "Парсинг и выполнение Func<DateTime?, bool>  с использование контекста")]
        public void DateTimeIsEmptyFuncCtxParametersParserTest()
        {
            var expression = "(new Func<DateTime?, bool>(x_=>!x_.HasValue||x_.Value.Date==DateTime.MinValue.Date).Invoke(ctx.Get<System.DateTime>(\"Название ключа\")))";

            var result = false;
            Func<IExpressionContext, bool> fn = null;
            Action action = () => fn = ExpressionParser
                                           .FromString(expression)
                                           .WithArgument<IExpressionContext>("ctx")
                                           .ToFunction<IExpressionContext, bool>();

            action.ShouldNotThrow();
            fn.Should().NotBeNull();

            var helper = new ExpressionContext();

            action = () => result = fn.Invoke(helper);

            helper.Set("Название ключа", DateTime.Now);
            action.ShouldNotThrow();
            result.Should().BeFalse();

            helper.Set("Название ключа", DateTime.MinValue);
            action.ShouldNotThrow();
            result.Should().BeTrue();

            helper.Set("Название ключа", DateTime.MinValue.AddHours(12));
            action.ShouldNotThrow();
            result.Should().BeTrue();

            helper.Set<object>("Название ключа", null);
            action.ShouldNotThrow();
            result.Should().BeTrue();
        }
    }
}