namespace Expresso
{
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using Expresso.Extensions;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal partial class ExpressionSyntaxVisitor
    {
        public override Expression VisitConditionalExpression(ConditionalExpressionSyntax node)
        {
            var test = node.Condition.Accept(this);
            var @true = node.WhenTrue.Accept(this);
            var @false = node.WhenFalse.Accept(this);

            return Expression.Condition(test, @true, @false);
        }

        public override Expression VisitIfStatement(IfStatementSyntax node)
        {
            var @if = Visit(node.Condition);
            var @else = node.Else != null && node.Else.Statement != null
                            ? Visit(node.Else.Statement)
                            : null;

            var body = Visit(node.Statement);

            return @else == null
                       ? Expression.IfThen(@if, body)
                       : Expression.IfThenElse(@if, body, @else);
        }

        public override Expression VisitElseClause(ElseClauseSyntax node)
        {
            return base.VisitElseClause(node);
        }

        public override Expression VisitSwitchStatement(SwitchStatementSyntax node)
        {
            var value = Visit(node.Expression);

            var @break = Expression.Label(value.Type);
            GetNamedStack<LabelTarget>(LoopBreak).Push(@break);

            Expression defaultBody = Expression.Label(@break, Expression.Default(value.Type));
            var cases = new List<SwitchCase>();
            foreach (var section in node.Sections)
            {
                var values = new List<Expression>();
                var statement = Visit(section.Statements.First());
                foreach (var labelSyntax in section.Labels)
                {
                    var caseSwitch = labelSyntax as CaseSwitchLabelSyntax;
                    if (caseSwitch != null)
                        values.Add(Visit(caseSwitch));

                    var defaultSwitch = labelSyntax as DefaultSwitchLabelSyntax;
                    if (defaultSwitch != null)
                        defaultBody = statement;                    
                }

                if (values.IsNotEmpty())
                {
                    cases.Add(Expression.SwitchCase(statement, values));
                }
            }

            GetNamedStack<LabelTarget>(LoopBreak).Pop();

            var @switch = defaultBody != null
                              ? Expression.Switch(value, defaultBody, cases.ToArray())
                              : Expression.Switch(value, cases.ToArray());

            return Expression.Block(@switch, Expression.Label(@break, Expression.Default(value.Type)));
        }

        public override Expression VisitSwitchSection(SwitchSectionSyntax node)
        {
            return base.VisitSwitchSection(node);
        }

        public override Expression VisitCaseSwitchLabel(CaseSwitchLabelSyntax node)
        {
            return Visit(node.Value);
        }

        public override Expression VisitDefaultSwitchLabel(DefaultSwitchLabelSyntax node)
        {
            return null;
        }
    }
}