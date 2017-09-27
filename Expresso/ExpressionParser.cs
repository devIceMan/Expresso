namespace Expresso
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using Expresso.ExpressionToCode;
    using Expresso.Extensions;
    using Expresso.Utils;
    using JetBrains.Annotations;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using LinqExpression = System.Linq.Expressions.Expression;

    /// <summary>
    /// Парсер строк в выражения с помощью Roslyn
    /// </summary>
    public class ExpressionParser : AbstractParser<ExpressionParser>
    {
        /// <summary>
        /// Список глобальных конфигураторов парсера
        /// </summary>
        public static readonly List<ExpressionParserConfigurator> GlobalConfigurators = new List<ExpressionParserConfigurator>();

        private static readonly Regex LambdaDeclaration = new Regex(@"^\s*(\(*)((,?\w*\s*)*)(\)*)\s*=>(\s*{?).*(\s*}?)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private readonly IList<ParameterExpression> _parameters = new List<ParameterExpression>();

        /// <summary>
        /// Приватный констрктор класса
        /// </summary>
        /// <param name="expression"> Выражие для разбора </param>
        private ExpressionParser([NotNull] string expression)
        {
            ArgumentChecker.NotEmpty(expression, nameof(expression));

            Expression = expression;
        }

        /// <summary>
        /// Обрабатываемое выражение
        /// </summary>
        public string Expression { get; }

        /// <summary>
        /// Создать новый экземпляр
        /// <see cref="ExpressionParser" />
        /// для разбора выражения
        /// <see cref="expression" />
        /// </summary>
        /// <param name="expression"> Разбираемое выражение </param>
        public static ExpressionParser FromString(string expression)
        {
            return new ExpressionParser(expression);
        }

        /// <summary>
        /// Получить строковое представление выражения
        /// </summary>
        /// <param name="expression"> Выражение </param>
        public static string ToString(Expression expression)
        {
            var walker = new ExpressionWalker();

            walker.RegisterConstantProcessor<TimespanConstantProcessor>();
            walker.RegisterConstantProcessor<SimpleStructProcessor>();
            walker.RegisterConstantProcessor<EnumProcessor>();

            //walker.RegisterConstantProcessor<DynamicDictionaryProcessor>();

            walker.RegisterConstantProcessor<SimpleGenericListProcessor>();

            walker.RegisterConstantProcessor<AnonymousObjectProcessor>();
            walker.RegisterConstantProcessor<SimpleObjectProcessor>();

            return walker.Visit(expression).Key;
        }

        /// <summary>
        /// Регистрация входного параметра выражения
        /// </summary>
        /// <param name="type"> Тип параметра </param>
        /// <param name="name"> Наименование параметра </param>
        public ExpressionParser WithArgument(Type type, string name = null)
        {
            ArgumentChecker.NotNull(type, nameof(type));

            _parameters.Add(LinqExpression.Parameter(type, name));

            return this;
        }

        /// <summary>
        /// Регистрация входного параметра выражения
        /// </summary>
        /// <typeparam name="T"> Тип параметра </typeparam>
        /// <param name="name"> Наименование параметра </param>
        public ExpressionParser WithArgument<T>(string name = null)
        {
            return WithArgument(typeof(T), name);
        }

        /// <summary>
        /// Регистрация набора типов входных параметров
        /// </summary>
        /// <param name="types"> Набор типов входных параметров </param>
        public ExpressionParser WithArguments([NotNull] IEnumerable<Type> types)
        {
            ArgumentChecker.NotNull(types, nameof(types));

            foreach (var type in types)
                WithArgument(type);

            return this;
        }

        /// <summary>
        /// Регистрация набора типов входных параметров
        /// </summary>
        /// <param name="types"> Набор типов входных параметров </param>
        public ExpressionParser WithArguments(params Type[] types)
        {
            return WithArguments(types.ToList());
        }

        /// <summary>
        /// Сформировать выражение
        /// <see cref="LambdaExpression" />
        /// </summary>
        /// <param name="resultType"> Тип результата </param>
        public LambdaExpression ToLambda(Type resultType)
        {
            var expr = ParseExpression(resultType);
            var lambda = expr as LambdaExpression;

            if (resultType != typeof(void) && lambda != null && !resultType.IsAssignableFrom(lambda.ReturnType))
                expr = LinqExpression.Convert((expr as LambdaExpression).Body, resultType);

            switch (expr.NodeType)
            {
                case ExpressionType.MemberAccess:
                case ExpressionType.Call:
                case ExpressionType.Convert:
                    if (!resultType.IsAssignableFrom(expr.Type))
                        expr = LinqExpression.Convert(expr, resultType);

                    return LinqExpression.Lambda(expr, _parameters);

                case ExpressionType.Lambda:
                    return lambda;

                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Сформировать выражение
        /// <see cref="LambdaExpression" />
        /// </summary>
        /// <typeparam name="TResult"> Тип результата </typeparam>
        public LambdaExpression ToLambda<TResult>()
        {
            return ToLambda(typeof(TResult));
        }

        /// <summary>
        /// Построить выражение и скомпилировать его
        /// </summary>
        /// <typeparam name="TResult"> Тип результата </typeparam>
        public ParamsFunction<TResult> ToParamsFunction<TResult>()
        {
            var fn = ToLambda<TResult>().Compile();
            return x => (TResult) fn.DynamicInvoke(x);
        }

        /// <summary>
        /// Построить выражение и скомпилировать его
        /// </summary>
        /// <typeparam name="TInput"> Тип аргумента </typeparam>
        /// <typeparam name="TResult"> Тип результата </typeparam>
        public Func<TInput, TResult> ToFunction<TInput, TResult>()
        {
            var fn = ToLambda<TResult>().Compile();
            return (Func<TInput, TResult>) fn;
        }

        /// <summary>
        /// Построить выражение и скомпилировать его
        /// </summary>
        /// <typeparam name="TResult"> Тип результата </typeparam>
        /// <returns> </returns>
        public Func<TResult> ToFunction<TResult>()
        {
            var fn = ToLambda<TResult>().Compile();
            return (Func<TResult>) fn;
        }

        /// <summary>
        /// Построить выражение и скомпилировать его
        /// </summary>
        /// <typeparam name="TInput"> Тип аргумента </typeparam>
        public Predicate<TInput> ToPredicate<TInput>()
        {
            var fn = ToLambda<bool>().Compile();
            return (Predicate<TInput>) fn;
        }

        /// <summary>
        /// Сформировать и выполнить выражение
        /// </summary>
        /// <param name="resultType"> Тип результата </param>
        /// <param name="params"> Параметры выполненеия </param>
        public object Call(Type resultType, params object[] @params)
        {
            var expression = ToLambda(resultType);
            var function = expression.Compile();
            return function.DynamicInvoke(@params);
        }

        /// <summary>
        /// Сформировать и выполнить выражение
        /// </summary>
        /// <typeparam name="TResult"> Тип результата </typeparam>
        /// <param name="params"> Параметры выполненеия </param>
        /// <returns> </returns>
        public TResult Call<TResult>(params object[] @params)
        {
            var result = Call(typeof(TResult), @params);
            return (TResult) result;
        }

        private static ParameterExpression UseOrCreateParameter(ParameterExpression p, ParameterSyntax l)
        {
            return string.IsNullOrWhiteSpace(p.Name) ? LinqExpression.Parameter(p.Type, l.Identifier.Text) : p;
        }

        private static ParameterSyntax[] GetLambdaParameters(LambdaExpressionSyntax lambda)
        {
            var simple = lambda as SimpleLambdaExpressionSyntax;
            if (simple != null)
                return new[] {simple.Parameter};

            var parenthesized = lambda as ParenthesizedLambdaExpressionSyntax;
            if (parenthesized != null)
                return parenthesized.ParameterList.Parameters.ToArray();

            throw new NotImplementedException();
        }

        private string BuildCodeForParsing(Type resultType)
        {
            var sb = new StringBuilder().AppendLine("namespace G {");

            sb.AppendLine("class C {");

            if (LambdaDeclaration.IsMatch(Expression))
            {
                var paramsString = _parameters.Count > 0
                    ? string.Join(",", _parameters.Select(x => TypeUtils.CSharpTypeName(x.Type)))
                    : string.Empty;

                sb.Append("Expression<");

                if (resultType == typeof(void))
                {
                    sb.Append("Action");
                    if (paramsString.IsNotEmpty())
                        sb.Append("<" + paramsString + ">");
                }
                else
                {
                    sb.Append("Func<");
                    if (paramsString.IsNotEmpty())
                        sb.Append(paramsString + ",");

                    sb.Append(TypeUtils.CSharpTypeName(resultType) + ">");
                }

                sb.Append(">")
                    .AppendLine(" M(){")
                    .AppendFormat("return {0};", Expression)
                    .AppendLine()
                    .AppendLine("}");
            }
            else
            {
                var paramsString = _parameters.Count > 0
                    ? string.Join(",", _parameters.Select(x => string.Format("{0} {1}", TypeUtils.CSharpTypeName(x.Type), x.Name)))
                    : string.Empty;

                sb.Append(TypeUtils.CSharpTypeName(resultType))
                    .AppendFormat(" M({0}){{", paramsString)
                    .AppendFormat("return {0}", Expression)
                    .Append("}");
            }

            sb.Append("}}");

            return sb.ToString();
        }

        private string GetExpressionKey(ParameterExpression[] parameterExpressions, Type resultType)
        {
            var sb = new StringBuilder();
            sb.Append(Expression.Replace(" ", string.Empty).Replace("\t", string.Empty).Replace(Environment.NewLine, string.Empty))
                .Append(";")
                .Append(parameterExpressions.Aggregate(string.Empty, (c, n) => c + ";" + n.Name + ":" + n.Type.FullName)).Append(";")
                .Append(resultType.FullName);

            return sb.ToString();
        }

        protected override Assembly[] CollectAssemblies()
        {
            var assemblies = base.CollectAssemblies().ToList();
            CollectAssembliesFromTypes(_parameters.Select(x => x.Type), assemblies);

            return assemblies.ToArray();
        }

        private Expression ParseExpression(Type resultType)
        {
            foreach (var configurator in GlobalConfigurators)
            {
                configurator.Invoke(this);
            }

            var text = BuildCodeForParsing(resultType);
            var tree = GetSyntaxTree(text);
            var returnStatements = tree.GetRoot()
                .DescendantNodes()
                .OfType<ReturnStatementSyntax>();

            var syntaxNode = returnStatements.First();

            SyntaxNode nodeExpression = syntaxNode.Expression;
            var lambda = nodeExpression as LambdaExpressionSyntax;

            var semanticModel = GetCompilation(tree, "ExpressionParserAssembly", new[] {resultType}).GetSemanticModel(tree);

            var parameterExpressions = _parameters.ToArray();
            if (lambda != null)
            {
                var @params = GetLambdaParameters(lambda);

                if (_parameters.Count != @params.Length)
                    throw new Exception($"Несоответствие числа параметров - ожидалось {_parameters.Count}, передано {@params.Length}");

                // если в аргументах передан неименованный параметр, необходимо сформировать новый набор параметров
                parameterExpressions = _parameters
                    .Select((p, idx) => UseOrCreateParameter(p, @params[idx]))
                    .ToArray();

                nodeExpression = lambda.Body;
            }

            var parsedExpression = Visit(nodeExpression, semanticModel, parameterExpressions);
            return LinqExpression.Lambda(parsedExpression, parameterExpressions);
        }

        private Expression Visit(SyntaxNode expression, SemanticModel model, ParameterExpression[] expressionParameters)
        {
            var visitor = new ExpressionSyntaxVisitor(null, model, expressionParameters, ResolutionService);
            return visitor.Visit(expression);
        }
    }
}