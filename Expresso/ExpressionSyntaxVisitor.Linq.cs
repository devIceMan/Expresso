namespace Expresso
{
    using System.Linq.Expressions;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal partial class ExpressionSyntaxVisitor
    {
        public override Expression VisitQueryExpression(QueryExpressionSyntax node)
        {
            return base.VisitQueryExpression(node);
        }

        public override Expression VisitQueryBody(QueryBodySyntax node)
        {
            return base.VisitQueryBody(node);
        }

        public override Expression VisitFromClause(FromClauseSyntax node)
        {
            return base.VisitFromClause(node);
        }

        public override Expression VisitLetClause(LetClauseSyntax node)
        {
            return base.VisitLetClause(node);
        }

        public override Expression VisitJoinClause(JoinClauseSyntax node)
        {
            return base.VisitJoinClause(node);
        }

        public override Expression VisitJoinIntoClause(JoinIntoClauseSyntax node)
        {
            return base.VisitJoinIntoClause(node);
        }

        public override Expression VisitWhereClause(WhereClauseSyntax node)
        {
            return base.VisitWhereClause(node);
        }

        public override Expression VisitOrderByClause(OrderByClauseSyntax node)
        {
            return base.VisitOrderByClause(node);
        }

        public override Expression VisitOrdering(OrderingSyntax node)
        {
            return base.VisitOrdering(node);
        }

        public override Expression VisitSelectClause(SelectClauseSyntax node)
        {
            return base.VisitSelectClause(node);
        }

        public override Expression VisitGroupClause(GroupClauseSyntax node)
        {
            return base.VisitGroupClause(node);
        }

        public override Expression VisitQueryContinuation(QueryContinuationSyntax node)
        {
            return base.VisitQueryContinuation(node);
        }
    }
}