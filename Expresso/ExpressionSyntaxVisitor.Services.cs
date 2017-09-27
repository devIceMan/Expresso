namespace Expresso
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Expresso.Extensions;
    using Expresso.Utils;
    using JetBrains.Annotations;

    internal partial class ExpressionSyntaxVisitor
    {
        private readonly IDictionary<string, object> _dynamics = new Dictionary<string, object>();

        /// <summary>
        /// Получение стека элементов типа
        /// <see cref="T" />
        /// по имени
        /// </summary>
        /// <typeparam name="T"> Тип элементов стека </typeparam>
        /// <param name="name"> Наименование стека </param>
        /// <returns> </returns>
        [NotNull]
        private Stack<T> GetNamedStack<T>(string name)
        {
            ArgumentChecker.NotEmpty(name, nameof(name));

            return _dynamics.GetOrCreate("STACK-" + name, () => new Stack<T>());
        }

        /// <summary>
        /// Получение очереди элементов типа
        /// <see cref="T" />
        /// по имени
        /// </summary>
        /// <typeparam name="T"> Тип элементов </typeparam>
        /// <param name="name"> Наименование очереди </param>
        /// <returns> </returns>
        [NotNull]
        private Queue<T> GetNamedQueue<T>(string name)
        {
            ArgumentChecker.NotEmpty(name, nameof(name));

            return _dynamics.GetOrCreate("QUEUE-" + name, () => new Queue<T>());
        }

        /// <summary>
        /// Зарегистрировать переменные в стеке
        /// </summary>
        /// <param name="expressions"> Набор регистрируемых переменных </param>
        /// <returns> </returns>
        private int RegisterParam(params Expression[] expressions)
        {
            var count = 0;

            foreach (var expression in expressions)
            {
                var parameter = expression as ParameterExpression;
                if (parameter != null)
                {
                    GetNamedStack<ParameterExpression>(ParameterExpressions).Push(parameter);
                    count++;
                }

                var block = expression as BlockExpression;
                if (block != null)
                {
                    count += block.Expressions.Sum(expr => RegisterParam(expr));
                    count += block.Variables.Sum(variable => RegisterParam(variable));
                }

                var wrapper = expression as VariableBlockWrapper;
                if (wrapper != null)
                {
                    count += wrapper.Expressions.Sum(expr => RegisterParam(expr));
                    count += wrapper.Variables.Sum(variable => RegisterParam(variable));
                }
            }

            return count;
        }
    }
}