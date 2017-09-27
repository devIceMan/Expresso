namespace Expresso
{
    using System.Linq.Expressions;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal partial class ExpressionSyntaxVisitor
    {
        public override Expression VisitThrowStatement(ThrowStatementSyntax node)
        {
            var expression = Visit(node.Expression);
            return Expression.Throw(expression);
        }

        public override Expression VisitTryStatement(TryStatementSyntax node)
        {
            return base.VisitTryStatement(node);
        }

        public override Expression VisitCatchClause(CatchClauseSyntax node)
        {
            return base.VisitCatchClause(node);
        }

        public override Expression VisitCatchDeclaration(CatchDeclarationSyntax node)
        {
            return base.VisitCatchDeclaration(node);
        }

        public override Expression VisitCatchFilterClause(CatchFilterClauseSyntax node)
        {
            return base.VisitCatchFilterClause(node);
        }

        public override Expression VisitFinallyClause(FinallyClauseSyntax node)
        {
            return base.VisitFinallyClause(node);
        }
    }
}