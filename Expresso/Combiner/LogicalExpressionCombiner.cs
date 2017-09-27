namespace Expresso.Combiner
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Linq.Expressions;
    using Expresso.Utils;

    /// <summary>
    /// Класс-расширение для объединения логических лямбд
    /// </summary>
    public static class LogicalExpressionCombiner
    {
        /// <summary>
        /// Метод-расширение, объединяет два логических лямбда-выражения с помощью "И"
        /// </summary>
        /// <param name="left">Левое лямбда-выражение</param>
        /// <param name="right">Правое лямбда-выражение</param>
        /// <returns>Объединенное выражение</returns>
        public static LambdaExpression And(this LambdaExpression left, LambdaExpression right)
        {
            return Combine(Expression.AndAlso, left, right);
        }

        /// <summary>
        /// Метод-расширение, объединяет два логических лямбда-выражения с помощью "ИЛИ"
        /// </summary>
        /// <param name="left">Левое лямбда-выражение</param>
        /// <param name="right">Правое лямбда-выражение</param>
        /// <returns>Объединенное выражение</returns>
        public static LambdaExpression Or(this LambdaExpression left, LambdaExpression right)
        {
            return Combine(Expression.OrElse, left, right);
        }

        private static LambdaExpression Combine(Func<Expression, Expression, BinaryExpression> concatenator, LambdaExpression left, LambdaExpression right)
        {
            // Если один из переданных параметров - null, то отдаем другой.
            if (left == null)
            {
                return right;
            }

            if (right == null)
            {
                return left;
            }

            // Проверяем возвращаемый тип лямбда-выражений, оба должны быть логическими
            if (left.ReturnType != typeof(bool) || right.ReturnType != typeof(bool))
            {
                throw new Exception("Not a logical expression");
            }

            // Получаем список параметров из обоих выражений (без повторений)
            var mergedParameters = MergeParameters(left.Parameters, right.Parameters);

            var body = concatenator(left.Body, right.Body);

            // Заменяем параметры в правом выражении на аналогичные из левого (по имени)
            var visitor = new ParameterReplaceVisitor(mergedParameters);
            var replacedBody = visitor.Visit(body);

            var lambda = Expression.Lambda(replacedBody, mergedParameters);

            return lambda;
        }

        private static ParameterExpression[] MergeParameters(ReadOnlyCollection<ParameterExpression> leftParameters, ReadOnlyCollection<ParameterExpression> rightParameters)
        {
            ArgumentChecker.NotNull(leftParameters, nameof(leftParameters));
            ArgumentChecker.NotNull(rightParameters, nameof(rightParameters));

            var mergedParameters = new List<ParameterExpression>();

            if (!leftParameters.Any())
            {
                return rightParameters.ToArray();
            }

            if (!rightParameters.Any())
            {
                return leftParameters.ToArray();
            }

            var jointParameters = rightParameters.Select(x => x.Name).Intersect(leftParameters.Select(y => y.Name));

            mergedParameters.AddRange(leftParameters.Where(x => jointParameters.All(y => y != x.Name)));
            mergedParameters.AddRange(rightParameters.Where(x => jointParameters.All(y => y != x.Name)));

            foreach (var @param in jointParameters)
            {
                var leftParam = leftParameters.First(x => x.Name == @param);
                var rightParam = rightParameters.First(x => x.Name == @param);

                if (leftParam.Type != rightParam.Type)
                {
                    throw new Exception("Different type on same name parameter");
                }

                mergedParameters.Add(leftParam);
            }

            return mergedParameters.ToArray();
        }
    }
}