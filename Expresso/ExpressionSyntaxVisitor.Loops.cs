namespace Expresso
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Expresso.Extensions;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal partial class ExpressionSyntaxVisitor
    {
        public override Expression VisitYieldStatement(YieldStatementSyntax node)
        {
            return base.VisitYieldStatement(node);
        }

        public override Expression VisitWhileStatement(WhileStatementSyntax node)
        {
            return base.VisitWhileStatement(node);
        }

        public override Expression VisitDoStatement(DoStatementSyntax node)
        {
            return base.VisitDoStatement(node);
        }

        public override Expression VisitForStatement(ForStatementSyntax node)
        {
            var @continue = Expression.Label();
            var @break = Expression.Label();

            GetNamedStack<LabelTarget>(LoopContinue).Push(@continue);
            GetNamedStack<LabelTarget>(LoopBreak).Push(@break);

            var declaration = Visit(node.Declaration);
            RegisterParam(declaration);

            var body = Visit(node.Statement);

            var condition = Visit(node.Condition);

            var blockExpressions = new List<Expression>();
            var inBlockVariables = new List<ParameterExpression>();

            var wrapper = declaration as VariableBlockWrapper;
            if (wrapper != null)
            {
                blockExpressions.AddRange(wrapper.Expressions);
                inBlockVariables.AddRange(wrapper.Variables);
            }

            blockExpressions.AddRange(node.Initializers.Select(Visit));

            var loopExpressions = new List<Expression> {
                body
            };

            loopExpressions.AddRange(node.Incrementors.Select(Visit));           

            var check = Expression.IfThenElse(condition, Expression.Block(loopExpressions), Expression.Break(@break));
            blockExpressions.Add(Expression.Loop(check, @break, @continue));

            return Expression.Block(inBlockVariables, blockExpressions);
        }

        public override Expression VisitForEachStatement(ForEachStatementSyntax node)
        {
            var @break = Expression.Label();
            var @continue = Expression.Label();

            GetNamedStack<LabelTarget>(LoopContinue).Push(@continue);
            GetNamedStack<LabelTarget>(LoopBreak).Push(@break);

            var enumerable = Visit(node.Expression);

            var interfaces = enumerable.Type.GetInterfaces();
            if (enumerable.Type.IsInterface)
            {
                interfaces = new[] { enumerable.Type }.Concat(interfaces).ToArray();
            }

            var pair = interfaces
                .Where(x => x.IsGenericType)
                .Select(
                    x => new {
                                 Definition = x.GetGenericTypeDefinition(),
                                 Type = x
                             })
                .First(x => x.Definition == typeof(IEnumerable<>));

            var elementType = pair.Type.GetGenericArguments()[0] ?? typeof(object);
            var getEnumerator = pair.Type.GetMethod("GetEnumerator");
            var enumeratorType = typeof(IEnumerator<>).MakeGenericType(elementType);

            var varCurrent = Expression.Parameter(elementType, node.Identifier.Text);
            var varEnumerator = Expression.Variable(enumeratorType, "enumerator");
            RegisterParam(varEnumerator, varCurrent);

            var moveNextMethod = typeof(IEnumerator).GetMethod("MoveNext");

            var body = Visit(node.Statement);

            var enumeratorCheck = Expression.NotEqual(Expression.Call(varEnumerator, moveNextMethod), Expression.Constant(false));
            var enumeratorBody = Expression.Block(
                Expression.Assign(varCurrent, Expression.Property(varEnumerator, "Current")),
                body,
                Expression.Continue(@continue));

            var check = Expression.IfThenElse(enumeratorCheck, enumeratorBody, Expression.Break(@break));

            GetNamedStack<LabelTarget>(LoopContinue).Pop();
            GetNamedStack<LabelTarget>(LoopBreak).Pop();

            var blockVariables = new[] { varEnumerator, varCurrent };
            var blockExpressions = new Expression[] {
                                                        Expression.Assign(varEnumerator, Expression.Call(enumerable, getEnumerator)),
                                                        Expression.Loop(check, @break, @continue)
                                                    };

            return Expression.Block(blockVariables, blockExpressions);
        }
    }
}