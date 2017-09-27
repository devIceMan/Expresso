namespace Expresso
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal partial class ExpressionSyntaxVisitor
    {
        public override Expression VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpressionSyntax node)
        {
            // получаем описани типа
            var typeInfo = _semanticModel.GetTypeInfo(node);
            var type = ResolveType(typeInfo.Type, node);

            if (!node.Initializers.Any())
            {
                return Expression.New(type);
            }

            var arguments = new List<Expression>();

            foreach (var declarer in node.Initializers)
            {
                var expression = declarer.Expression;

                if (expression is AssignmentExpressionSyntax)
                {
                    var x = declarer.Expression.Accept(this);
                    arguments.Add(x);
                }
                else if (expression is MemberAccessExpressionSyntax)
                {
                    var member = expression as MemberAccessExpressionSyntax;
                    var right = VisitMemberAccessExpression(member);
                    arguments.Add(right);
                }
                else if (expression is BinaryExpressionSyntax)
                {
                    var binary = expression as BinaryExpressionSyntax;
                    var right = Visit(binary);
                    arguments.Add(right);
                }
                else if (expression is IdentifierNameSyntax)
                {
                    var x = declarer.Expression.Accept(this);
                    arguments.Add(x);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            var ctor = type.GetConstructors().First(x => x.GetParameters().Any());
            var members = type.GetProperties().Cast<MemberInfo>().ToArray();
            var result = Expression.New(ctor, arguments, members);
            return result;
        }

        public override Expression VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
        {
            return base.VisitAnonymousMethodExpression(node);
        }

        public override Expression VisitAnonymousObjectMemberDeclarator(AnonymousObjectMemberDeclaratorSyntax node)
        {
            return base.VisitAnonymousObjectMemberDeclarator(node);
        }
    }
}