namespace Expresso
{
    using System.Linq.Expressions;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal partial class ExpressionSyntaxVisitor
    {
        public override Expression VisitDocumentationCommentTrivia(DocumentationCommentTriviaSyntax node)
        {
            return base.VisitDocumentationCommentTrivia(node);
        }

        public override Expression VisitTypeCref(TypeCrefSyntax node)
        {
            return base.VisitTypeCref(node);
        }

        public override Expression VisitQualifiedCref(QualifiedCrefSyntax node)
        {
            return base.VisitQualifiedCref(node);
        }

        public override Expression VisitNameMemberCref(NameMemberCrefSyntax node)
        {
            return base.VisitNameMemberCref(node);
        }

        public override Expression VisitIndexerMemberCref(IndexerMemberCrefSyntax node)
        {
            return base.VisitIndexerMemberCref(node);
        }

        public override Expression VisitOperatorMemberCref(OperatorMemberCrefSyntax node)
        {
            return base.VisitOperatorMemberCref(node);
        }

        public override Expression VisitConversionOperatorMemberCref(ConversionOperatorMemberCrefSyntax node)
        {
            return base.VisitConversionOperatorMemberCref(node);
        }

        public override Expression VisitCrefParameterList(CrefParameterListSyntax node)
        {
            return base.VisitCrefParameterList(node);
        }

        public override Expression VisitCrefBracketedParameterList(CrefBracketedParameterListSyntax node)
        {
            return base.VisitCrefBracketedParameterList(node);
        }

        public override Expression VisitCrefParameter(CrefParameterSyntax node)
        {
            return base.VisitCrefParameter(node);
        }

        public override Expression VisitXmlElement(XmlElementSyntax node)
        {
            return base.VisitXmlElement(node);
        }

        public override Expression VisitXmlElementStartTag(XmlElementStartTagSyntax node)
        {
            return base.VisitXmlElementStartTag(node);
        }

        public override Expression VisitXmlElementEndTag(XmlElementEndTagSyntax node)
        {
            return base.VisitXmlElementEndTag(node);
        }

        public override Expression VisitXmlEmptyElement(XmlEmptyElementSyntax node)
        {
            return base.VisitXmlEmptyElement(node);
        }

        public override Expression VisitXmlName(XmlNameSyntax node)
        {
            return base.VisitXmlName(node);
        }

        public override Expression VisitXmlPrefix(XmlPrefixSyntax node)
        {
            return base.VisitXmlPrefix(node);
        }

        public override Expression VisitXmlTextAttribute(XmlTextAttributeSyntax node)
        {
            return base.VisitXmlTextAttribute(node);
        }

        public override Expression VisitXmlCrefAttribute(XmlCrefAttributeSyntax node)
        {
            return base.VisitXmlCrefAttribute(node);
        }

        public override Expression VisitXmlNameAttribute(XmlNameAttributeSyntax node)
        {
            return base.VisitXmlNameAttribute(node);
        }

        public override Expression VisitXmlText(XmlTextSyntax node)
        {
            return base.VisitXmlText(node);
        }

        public override Expression VisitXmlCDataSection(XmlCDataSectionSyntax node)
        {
            return base.VisitXmlCDataSection(node);
        }

        public override Expression VisitXmlProcessingInstruction(XmlProcessingInstructionSyntax node)
        {
            return base.VisitXmlProcessingInstruction(node);
        }

        public override Expression VisitXmlComment(XmlCommentSyntax node)
        {
            return base.VisitXmlComment(node);
        }
    }
}