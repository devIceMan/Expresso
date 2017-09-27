namespace Expresso.ExpressionToCode
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text;
    using Expresso.Extensions;
    using Fasterflect;

    internal class ExpressionWalker
    {
        private const int LevelParameter = 0;
        private const int LevelConstant = 0;

        private const int LevelMemberAccess = 1;
        private const int LevelArrayIndex = 1;
        private const int LevelNew = 1;
        private const int LevelMemberInit = 1;
        private const int LevelListBinding = 1;
        private const int LevelMethodCall = 1;
        private const int LevelConvert = 1;

        private const int LevelMultiply = 5;
        private const int LevelDivide = 5;
        private const int LevelModulo = 5;

        private const int LevelAdd = 6;
        private const int LevelSubtract = 6;
        private const int LevelExclusiveOr = 6;

        private const int LevelRightShift = 7;
        private const int LevelLeftShift = 7;

        private const int LevelLessThan = 8;
        private const int LevelLessThanOrEqual = 8;
        private const int LevelGreaterThan = 8;
        private const int LevelGreaterThanOrEqual = 8;

        private const int LevelEqual = 9;
        private const int LevelNotEqual = 9;

        private const int LevelAnd = 13;

        private const int LevelOr = 14;

        private const int LevelCoalesce = 15;

        private const int LevelLambda = 16;

        private const int LevelComma = 17;

        private static readonly Dictionary<Type, string> TypeAliases = new Dictionary<Type, string> {
            {typeof(byte), "byte"},
            {typeof(sbyte), "sbyte"},
            {typeof(short), "short"},
            {typeof(ushort), "ushort"},
            {typeof(int), "int"},
            {typeof(uint), "uint"},
            {typeof(long), "long"},
            {typeof(ulong), "ulong"},
            {typeof(float), "float"},
            {typeof(double), "double"},
            {typeof(decimal), "decimal"},
            {typeof(object), "object"},
            {typeof(bool), "bool"},
            {typeof(char), "char"},
            {typeof(string), "string"},
            {typeof(void), "void"},
            {typeof(byte?), "byte?"},
            {typeof(sbyte?), "sbyte?"},
            {typeof(short?), "short?"},
            {typeof(ushort?), "ushort?"},
            {typeof(int?), "int?"},
            {typeof(uint?), "uint?"},
            {typeof(long?), "long?"},
            {typeof(ulong?), "ulong?"},
            {typeof(float?), "float?"},
            {typeof(double?), "double?"},
            {typeof(decimal?), "decimal?"},
            {typeof(bool?), "bool?"},
            {typeof(char?), "char?"}
        };

        private readonly IList<ConstantExpressionProcessor> _constantParsers = new List<ConstantExpressionProcessor>();

        internal ExpressionWalker RegisterConstantProcessor<T>()
            where T : ConstantExpressionProcessor
        {
            var parser = (ConstantExpressionProcessor) Activator.CreateInstance(typeof(T), this);
            _constantParsers.Add(parser);

            return this;
        }

        internal KeyValuePair<string, int> Visit(Expression exp)
        {
            switch (exp.NodeType)
            {
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.ArrayLength:
                case ExpressionType.Quote:
                case ExpressionType.TypeAs:
                    return VisitUnary((UnaryExpression) exp);
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.Coalesce:
                case ExpressionType.ArrayIndex:
                case ExpressionType.RightShift:
                case ExpressionType.LeftShift:
                case ExpressionType.ExclusiveOr:
                    return VisitBinary((BinaryExpression) exp);
                case ExpressionType.TypeIs:
                    return VisitTypeIs((TypeBinaryExpression) exp);
                case ExpressionType.Conditional:
                    return VisitConditional((ConditionalExpression) exp);
                case ExpressionType.Constant:
                    return VisitConstant((ConstantExpression) exp);
                case ExpressionType.Parameter:
                    return VisitParameter((ParameterExpression) exp);
                case ExpressionType.MemberAccess:
                    return VisitMemberAccess((MemberExpression) exp);
                case ExpressionType.Call:
                    return VisitMethodCall((MethodCallExpression) exp);
                case ExpressionType.Lambda:
                    return VisitLambda((LambdaExpression) exp);
                case ExpressionType.New:
                    return VisitNew((NewExpression) exp);
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    return VisitNewArray((NewArrayExpression) exp);
                case ExpressionType.Invoke:
                    return VisitInvocation((InvocationExpression) exp);
                case ExpressionType.MemberInit:
                    return VisitMemberInit((MemberInitExpression) exp);
                case ExpressionType.ListInit:
                    return VisitListInit((ListInitExpression) exp);
                default:
                    throw new Exception($"Unhandled expression type: '{exp.NodeType}'");
            }
        }

        private static string Parenthesize(int targetLevel, KeyValuePair<string, int> value)
        {
            if (targetLevel >= value.Value)
            {
                return value.Key;
            }
            return "(" + value.Key + ")";
        }

        private static KeyValuePair<string, int> Parenthesize(int targetLevel, KeyValuePair<string, int> left, string operatorName, KeyValuePair<string, int> right)
        {
            return new KeyValuePair<string, int>(Parenthesize(targetLevel, left) + operatorName + Parenthesize(targetLevel, right), targetLevel);
        }

        private KeyValuePair<string, int> VisitListInit(ListInitExpression listInitExpression)
        {
            throw new NotImplementedException();
        }

        private KeyValuePair<string, int> VisitMemberInit(MemberInitExpression memberInitExpression)
        {
            var sb = new StringBuilder();

            sb.Append(Parenthesize(LevelNew, VisitNew(memberInitExpression.NewExpression)));
            sb.Append('{');
            sb.Append(string.Join(",", memberInitExpression.Bindings.Select(x => $"{x.Member.Name} = {Parenthesize(LevelComma, VisitBinding(x))}")));
            sb.Append('}');

            return new KeyValuePair<string, int>(sb.ToString(), LevelMemberInit);
        }

        private KeyValuePair<string, int> VisitBinding(MemberBinding binding)
        {
            switch (binding.BindingType)
            {
                case MemberBindingType.Assignment:
                    return Visit(((MemberAssignment) binding).Expression);
                case MemberBindingType.ListBinding:
                    return new KeyValuePair<string, int>(
                        "{" + string.Join(",", ((MemberListBinding) binding).Initializers.Select(x => string.Join(",", x.Arguments.Select(y => Parenthesize(LevelComma, Visit(y)))))) + "}",
                        LevelListBinding);
                case MemberBindingType.MemberBinding:
                default:
                    throw new NotImplementedException();
            }
        }

        private KeyValuePair<string, int> VisitInvocation(InvocationExpression invocationExpression)
        {
            throw new NotImplementedException();
        }

        private KeyValuePair<string, int> VisitNewArray(NewArrayExpression newArrayExpression)
        {
            throw new NotImplementedException();
        }

        private KeyValuePair<string, int> VisitNew(NewExpression newExpression)
        {
            var sb = new StringBuilder();

            sb.Append("new ");
            sb.Append(TypeUtils.CSharpTypeName(newExpression.Type));
            sb.AppendFormat("({0})", string.Join(", ", newExpression.Arguments.Select(x => Parenthesize(LevelComma, Visit(x)))));
            return new KeyValuePair<string, int>(sb.ToString(), LevelNew);
        }

        private KeyValuePair<string, int> VisitLambda(LambdaExpression lambdaExpression)
        {
            var sb = new StringBuilder();
            if (lambdaExpression.Parameters.Count == 1)
            {
                sb.Append(lambdaExpression.Parameters[0].Name);
            }
            else
            {
                sb.Append('(');
                sb.Append(string.Join(",", lambdaExpression.Parameters.Select(x => x.Name)));
                sb.Append(')');
            }

            sb.Append(" => ");

            sb.Append(Parenthesize(LevelLambda, Visit(lambdaExpression.Body)));
            return new KeyValuePair<string, int>(sb.ToString(), LevelLambda);
        }

        private KeyValuePair<string, int> VisitMethodCall(MethodCallExpression method)
        {
            var genericArgs = method.Method.GetGenericArguments().Select(AliasOrName).ToList();
            var args = new List<string>();

            var @object = method.Object;
            var arguments = method.Arguments.ToArray();

            if (method.Method.IsStatic)
            {
                if (method.Method.GetCustomAttributes(typeof(ExtensionAttribute), true).Any())
                {
                    @object = method.Arguments[0];
                    arguments = method.Arguments.Skip(1).ToArray();
                    genericArgs.Clear();
                }
                else
                {
                    @object = Expression.Constant(method.Method.DeclaringType);
                }
            }

            foreach (var t in arguments)
            {
                args.Add(Parenthesize(LevelComma, Visit(t)));
            }

            var sb = new StringBuilder()
                .Append(Parenthesize(LevelMethodCall, Visit(@object)))
                .Append($".{method.Method.Name}")
                .Append(genericArgs.IsEmpty() ? "" : $"<{string.Join(",", genericArgs)}>")
                .Append("(")
                .Append(string.Join(", ", args))
                .Append(")");

            return new KeyValuePair<string, int>(sb.ToString(), LevelMethodCall);
        }

        private bool MaybeClosureInvocation(Expression expression)
        {
            if (expression?.NodeType != ExpressionType.Constant)
                return false;

            var type = expression.Type;
            return type?.IsNested == true && type?.IsNestedPrivate == true && type?.Name.StartsWith("<>c__DisplayClass") == true;
        }

        private KeyValuePair<string, int> VisitMemberAccess(MemberExpression memberAccess)
        {
            if (memberAccess.Expression != null)
            {
                var expr = memberAccess.Expression;
                if (memberAccess.Member.MemberType == MemberTypes.Field && MaybeClosureInvocation(memberAccess.Expression))
                {
                    var getter = Expression.Lambda<Func<object>>(Expression.Convert(expr, typeof(object))).Compile();
                    var @object = getter.Invoke();

                    var value = @object.GetFieldValue(memberAccess.Member.Name);
                    return Visit(Expression.Constant(value));
                }

                return new KeyValuePair<string, int>(
                    Parenthesize(LevelMemberAccess, Visit(memberAccess.Expression)) + "." + memberAccess.Member.Name,
                    LevelMemberAccess);
            }
            return new KeyValuePair<string, int>(
                $"{AliasOrName(memberAccess.Member.DeclaringType)}.{memberAccess.Member.Name}",
                LevelMemberAccess);
        }

        private KeyValuePair<string, int> VisitParameter(ParameterExpression parameterExpression)
        {
            return new KeyValuePair<string, int>(parameterExpression.Name, LevelParameter);
        }

        private KeyValuePair<string, int> VisitConstant(ConstantExpression @const)
        {
            string stringValue = null;
            var value = @const.Value;
            if (value == null)
                return new KeyValuePair<string, int>("null", LevelConstant);

            foreach (var parser in _constantParsers)
            {
                if (parser.CanProcess(@const, value))
                    stringValue = parser.Process(@const, value);
                if (stringValue.IsNotEmpty())
                    return new KeyValuePair<string, int>(stringValue, LevelConstant);
            }

            switch (Type.GetTypeCode(@const.Type))
            {
                case TypeCode.Empty:
                    break;
                case TypeCode.Object:
                    var typeValue = value as Type;
                    if (typeValue != null)
                    {
                        stringValue = AliasOrName(typeValue);
                    }
                    break;
                case TypeCode.DBNull:
                    break;
                case TypeCode.Boolean:
                    stringValue = $"{@const.Value}".ToLower();
                    break;
                case TypeCode.Char:
                    stringValue = $"'{@const.Value}'";
                    break;
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    stringValue = string.Format(CultureInfo.InvariantCulture, "{0}", @const.Value);
                    break;
                case TypeCode.DateTime:
                    var dt = (DateTime) @const.Value;
                    var strKind = Visit(Expression.Constant(dt.Kind));
                    stringValue = $"new DateTime({dt.Ticks}, {strKind.Key})";
                    break;
                case TypeCode.String:
                    stringValue = $"\"{@const.Value}\"";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (stringValue.IsNotEmpty())
                return new KeyValuePair<string, int>(stringValue, LevelConstant);

            throw new NotImplementedException();
        }

        private KeyValuePair<string, int> VisitConditional(ConditionalExpression conditionalExpression)
        {
            throw new NotImplementedException();
        }

        private KeyValuePair<string, int> VisitTypeIs(TypeBinaryExpression typeBinaryExpression)
        {
            throw new NotImplementedException();
        }

        private KeyValuePair<string, int> VisitBinary(BinaryExpression binaryExpression)
        {
            var left = Visit(binaryExpression.Left);
            var right = Visit(binaryExpression.Right);
            switch (binaryExpression.NodeType)
            {
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    return Parenthesize(LevelAdd, left, " + ", right);
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    return Parenthesize(LevelSubtract, left, " - ", right);
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    return Parenthesize(LevelMultiply, left, " * ", right);
                case ExpressionType.Divide:
                    return Parenthesize(LevelDivide, left, " / ", right);
                case ExpressionType.Modulo:
                    return Parenthesize(LevelModulo, left, " % ", right);

                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    return Parenthesize(LevelAnd, left, " && ", right);
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return Parenthesize(LevelOr, left, " || ", right);

                case ExpressionType.LessThan:
                    return Parenthesize(LevelLessThan, left, " < ", right);
                case ExpressionType.LessThanOrEqual:
                    return Parenthesize(LevelLessThanOrEqual, left, " <= ", right);
                case ExpressionType.GreaterThan:
                    return Parenthesize(LevelGreaterThan, left, " > ", right);
                case ExpressionType.GreaterThanOrEqual:
                    return Parenthesize(LevelGreaterThanOrEqual, left, " >= ", right);
                case ExpressionType.Equal:
                    return Parenthesize(LevelEqual, left, " == ", right);
                case ExpressionType.NotEqual:
                    return Parenthesize(LevelNotEqual, left, " != ", right);
                case ExpressionType.ArrayIndex:
                    return new KeyValuePair<string, int>(Parenthesize(LevelArrayIndex, left) + "[" + right + "]", LevelArrayIndex);
                case ExpressionType.RightShift:
                    return Parenthesize(LevelRightShift, left, " >> ", right);
                case ExpressionType.LeftShift:
                    return Parenthesize(LevelLeftShift, left, " << ", right);
                case ExpressionType.ExclusiveOr:
                    return Parenthesize(LevelExclusiveOr, left, " ^ ", right);
                case ExpressionType.Coalesce:
                    return Parenthesize(LevelCoalesce, left, " ?? ", right);

                default:
                    throw new ArgumentException($"Не известный тип бинарной операции {binaryExpression.NodeType}");
            }
        }

        private KeyValuePair<string, int> VisitUnary(UnaryExpression unaryExpression)
        {
            switch (unaryExpression.NodeType)
            {
                case ExpressionType.Convert:
                    return new KeyValuePair<string, int>($"({TypeUtils.CSharpTypeName(unaryExpression.Type)}){Parenthesize(LevelConvert, Visit(unaryExpression.Operand))}",
                        LevelConvert);
            }

            throw new NotImplementedException();
        }

        public static string AliasOrName(Type type)
        {
            return TypeAliases.ContainsKey(type) ? TypeAliases[type] : type.Name;
        }
    }
}