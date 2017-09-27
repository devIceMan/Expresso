namespace Expresso.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Expresso.Combiner;
    using Expresso.Tests.Data;
    using FluentAssertions;
    using Xunit;

    [Trait("Category", "Expresso - Combine")]
    public class LogicalExpressionsCombinerTests
    {        
        [Theory(DisplayName = "Объединение выражений с одинаковыми параметрами")]
        [InlineData(1, false)]
        [InlineData(6, true)]
        [InlineData(101, true)]
        public void ShouldCombineExpressionsWithSameNamedArguments(int p1, bool res)
        {
            Expression<Func<int, bool>> first = p => p > 5;
            Expression<Func<int, bool>> second = p => p < 7;
            Expression<Func<int, bool>> third = p => p > 100;

            var result = first.And(second).Or(third);

            Delegate compiledResult = null;
            Action action = () => compiledResult = result.Compile();

            action.ShouldNotThrow();

            compiledResult.Should().NotBeNull();

            compiledResult.DynamicInvoke(p1).Should().Be(res);
        }

        [Theory(DisplayName = "Объединение выражений с разными параметрами")]
        [InlineData(6, 6, 6, true)]
        [InlineData(8, 8, 8, false)]
        [InlineData(8, 8, 101, true)]
        public void ShouldCombineExpressionsWithDifferentNamedArguments(int p1, int p2, int p3, bool res)
        {
            Expression<Func<int, bool>> first = p => p > 5;
            Expression<Func<int, bool>> second = x => x < 7;
            Expression<Func<int, bool>> third = z => z > 100;

            var result = first
                .And(second)
                .Or(third);

            Delegate compiledResult = null;
            Action action = () => compiledResult = result.Compile();

            action.ShouldNotThrow();

            compiledResult.Should().NotBeNull();

            compiledResult.DynamicInvoke(p1, p2, p3).Should().Be(res);
        }

        [Fact(DisplayName = "Объединение выражений с одинаково именованными параметрами разного типа вызывает ошибку")]
        public void ShouldFailOnSameNamedButDifferentTypedArguments()
        {
            Expression<Func<int, bool>> first = p => p > 5;
            Expression<Func<string, bool>> second = p => p.Length > 5;
            Expression<Func<int, bool>> third = z => z > 100;

            Action action = () => first
                                      .And(second)
                                      .Or(third);

            action.ShouldThrow<Exception>();
        }

        [Fact(DisplayName = "Объединение двух выражений, одно из которых - null")]
        public void ShouldCombineExpressionAndNull()
        {
            Expression<Func<int, bool>> left = p => p > 5;

            var result = left.And(null);

            result.Should().BeSameAs(left);
        }

        [Fact(DisplayName = "Объединение двух выражений, хотя бы одно из которых не логическое, вызывает ошибку")]
        public void ShouldFailOnNonLogicalExpressionsCombine()
        {
            Expression<Func<int, int>> left = p => p + 5;
            Expression<Func<int, bool>> right = p => p > 5;

            Action action = () => left.And(right);

            action.ShouldThrow<Exception>();
        }

        [Fact(DisplayName = "Объединение двух выражений с комплексными параметрами")]
        public void ShouldCombineExpressionsWithComplexArguments()
        {
            Expression<Func<TestEntity, bool>> left = context => context.TotalSum < 20;
            Expression<Func<TestEntity, bool>> right = context => context.Nested.Any(x => x.ItemSum > 10);

            var result = left.And(right);

            Delegate compiledResult = null;
            Action action = () => compiledResult = result.Compile();

            action.ShouldNotThrow();

            compiledResult.Should().NotBeNull();

            var testData = new TestEntity {
                                              TotalSum = 50,
                                              Nested = new List<TestItem> {
                                                                              new TestItem { ItemSum = 10 },
                                                                              new TestItem { ItemSum = 20 }
                                                                          }
                                          };

            compiledResult.DynamicInvoke(testData).Should().Be(false);
        }
    }
}