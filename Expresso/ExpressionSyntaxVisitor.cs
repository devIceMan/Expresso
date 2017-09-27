namespace Expresso
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Expresso.Extensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using TypeInfo = Microsoft.CodeAnalysis.TypeInfo;

    internal partial class ExpressionSyntaxVisitor : CSharpSyntaxVisitor<Expression>
    {
        private const string BlockReturnQueue = "BlockReturn";

        private const string ParameterExpressions = "ParameterExpressions";

        private const string LoopContinue = "LoopContinue";

        private const string LoopBreak = "LoopBreak";

        private readonly ExpressionSyntaxVisitor _parent;

        private readonly TypeResolutionService _resolutionService;

        private readonly SemanticModel _semanticModel;

        public ExpressionSyntaxVisitor(ExpressionSyntaxVisitor parent, SemanticModel semanticModel, ParameterExpression[] inputParameters, TypeResolutionService resolutionService)
        {
            _parent = parent;
            _semanticModel = semanticModel;

            if (inputParameters.IsNotEmpty())
            {
                var parameters = GetNamedStack<ParameterExpression>(ParameterExpressions);
                foreach (var parameterExpression in inputParameters)
                {
                    parameters.Push(parameterExpression);
                }
            }

            _resolutionService = resolutionService;
        }

        public override Expression VisitSizeOfExpression(SizeOfExpressionSyntax node)
        {
            return base.VisitSizeOfExpression(node);
        }

        public override Expression Visit(SyntaxNode node)
        {
            // сохраняем текущее количество параметров/локальных переменных
            var parameters = GetNamedStack<ParameterExpression>(ParameterExpressions);
            var count = parameters.Count;

            var expression = base.Visit(node);

            // убираем из стека добавленные параметры/локальные переменные
            while (parameters.Count > count)
            {
                parameters.Pop();
            }

            return expression;
        }

        public override Expression VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.Expression.Kind() == SyntaxKind.SimpleMemberAccessExpression)
            {
                var method = (MemberAccessExpressionSyntax)node.Expression;

                var nameInfo = _semanticModel.GetSymbolInfo(method.Name);

                var symbol = nameInfo.Symbol;

                if (symbol == null || symbol.Kind != SymbolKind.Method)
                {
                    switch (nameInfo.CandidateReason)
                    {
                        case CandidateReason.Inaccessible:
                            throw new Exception(string.Format("Метод {0} не доступен", method.Name.Identifier.Text));

                        case CandidateReason.OverloadResolutionFailure:
                            symbol = nameInfo.CandidateSymbols.Cast<IMethodSymbol>().First(x => x.Parameters.Length == node.ArgumentList.Arguments.Count);

                            break;

                        default:

                            // возможно где-то в середине выражения используется динамически срезолвленный тип
                            throw new Exception(string.Format("Метод {0} не найден", method.Name.Identifier.Text));
                    }
                }

                var methodSymbol = (IMethodSymbol)symbol;
                switch (methodSymbol.MethodKind)
                {
                    case MethodKind.ReducedExtension:
                    {
                        var expression = method.Expression.Accept(this);
                        var argumentTypes = new List<Type>();
                        argumentTypes.Add(expression.Type);
                        argumentTypes.AddRange(methodSymbol.Parameters.Select(x => ResolveType(x.Type, node)));
                        var methodInfo = ResolveMethod((IMethodSymbol)symbol, argumentTypes.ToArray());
                        var arguments = new List<Expression>();
                        arguments.Add(expression);
                        arguments.AddRange(node.ArgumentList.Arguments.Select(x => x.Expression.Accept(this)));

                        ConvertMethodArguments(methodInfo, arguments);

                        return Expression.Call(methodInfo, arguments.ToArray());
                    }

                    case MethodKind.Ordinary:
                    case MethodKind.DelegateInvoke:
                    {
                        var @params = methodSymbol.Parameters.Select(x => ResolveType(x.Type, node)).ToArray();

                        var methodInfo = ResolveMethod(methodSymbol, @params.ToArray());

                        var arguments = node.ArgumentList.Arguments.Select(x => x.Expression.Accept(this)).ToList();

                        ConvertMethodArguments(methodInfo, arguments);

                        if (methodInfo.IsStatic)
                        {
                            return Expression.Call(null, methodInfo, arguments);
                        }

                        var expression = method.Expression.Accept(this);
                        return Expression.Call(expression, methodInfo, arguments);
                    }

                    default:
                        throw new NotImplementedException();
                }
            }

            throw new NotImplementedException();
        }

        private static void ConvertMethodArguments(MethodInfo methodInfo, List<Expression> arguments)
        {
            // при необходимости конвертим типы аргументов
            var parameters = methodInfo.GetParameters();
            for (var idx = 0; idx < parameters.Length; idx++)
            {
                var parameter = parameters[idx];
                var argument = arguments.Count > idx
                    ? arguments[idx]
                    : null;

                if (argument == null)
                {
                    if (!parameter.IsOptional)
                    {
                        throw new ArgumentException();
                    }

                    try
                    {
                        var value = parameter.DefaultValue;
                        arguments.Add(Expression.Convert(Expression.Constant(value), parameter.ParameterType));
                    }
                    catch (FormatException)
                    {
                        arguments.Add(Expression.Default(parameter.ParameterType));
                    }
                }

                if (argument != null && parameter.ParameterType != argument.Type)
                {
                    arguments[idx] = Expression.Convert(arguments[idx], parameter.ParameterType);
                }
            }
        }

        public override Expression VisitAccessorDeclaration(AccessorDeclarationSyntax node)
        {
            return base.VisitAccessorDeclaration(node);
        }

        public override Expression VisitParameterList(ParameterListSyntax node)
        {
            return base.VisitParameterList(node);
        }

        public override Expression VisitBracketedParameterList(BracketedParameterListSyntax node)
        {
            return base.VisitBracketedParameterList(node);
        }

        public override Expression VisitParameter(ParameterSyntax node)
        {
            return base.VisitParameter(node);
        }

        public override Expression VisitIncompleteMember(IncompleteMemberSyntax node)
        {
            return base.VisitIncompleteMember(node);
        }

        public override Expression VisitSkippedTokensTrivia(SkippedTokensTriviaSyntax node)
        {
            return base.VisitSkippedTokensTrivia(node);
        }

        public override Expression VisitIfDirectiveTrivia(IfDirectiveTriviaSyntax node)
        {
            return base.VisitIfDirectiveTrivia(node);
        }

        public override Expression VisitElifDirectiveTrivia(ElifDirectiveTriviaSyntax node)
        {
            return base.VisitElifDirectiveTrivia(node);
        }

        public override Expression VisitElseDirectiveTrivia(ElseDirectiveTriviaSyntax node)
        {
            return base.VisitElseDirectiveTrivia(node);
        }

        public override Expression VisitEndIfDirectiveTrivia(EndIfDirectiveTriviaSyntax node)
        {
            return base.VisitEndIfDirectiveTrivia(node);
        }

        public override Expression VisitRegionDirectiveTrivia(RegionDirectiveTriviaSyntax node)
        {
            return base.VisitRegionDirectiveTrivia(node);
        }

        public override Expression VisitEndRegionDirectiveTrivia(EndRegionDirectiveTriviaSyntax node)
        {
            return base.VisitEndRegionDirectiveTrivia(node);
        }

        public override Expression VisitErrorDirectiveTrivia(ErrorDirectiveTriviaSyntax node)
        {
            return base.VisitErrorDirectiveTrivia(node);
        }

        public override Expression VisitWarningDirectiveTrivia(WarningDirectiveTriviaSyntax node)
        {
            return base.VisitWarningDirectiveTrivia(node);
        }

        public override Expression VisitIndexerDeclaration(IndexerDeclarationSyntax node)
        {
            return base.VisitIndexerDeclaration(node);
        }

        public override Expression VisitAccessorList(AccessorListSyntax node)
        {
            return base.VisitAccessorList(node);
        }

        public override Expression VisitTypeArgumentList(TypeArgumentListSyntax node)
        {
            return base.VisitTypeArgumentList(node);
        }

        public override Expression VisitAliasQualifiedName(AliasQualifiedNameSyntax node)
        {            
            return base.VisitAliasQualifiedName(node);
        }

        public override Expression VisitPredefinedType(PredefinedTypeSyntax node)
        {
            var typeInfo = _semanticModel.GetTypeInfo(node);
            var type = ResolveType(typeInfo.Type, node);

            return Expression.Constant(type);
        }

        public override Expression VisitBracketedArgumentList(BracketedArgumentListSyntax node)
        {
            return base.VisitBracketedArgumentList(node);
        }

        public override Expression VisitArgument(ArgumentSyntax node)
        {
            return node.Expression.Accept(this);
        }

        public override Expression VisitNameColon(NameColonSyntax node)
        {
            return base.VisitNameColon(node);
        }

        public override Expression VisitArgumentList(ArgumentListSyntax node)
        {
            return base.VisitArgumentList(node);
        }

        public override Expression VisitArrayCreationExpression(ArrayCreationExpressionSyntax node)
        {
            return base.VisitArrayCreationExpression(node);
        }

        public override Expression VisitImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax node)
        {
            var initializers = node.Initializer.Expressions.Select(x => x.Accept(this)).ToArray();
            var isSigleType = initializers.Length == 1 || initializers.Select(x => x.Type).Distinct().Count() == 1;
            var arrayType = isSigleType
                                ? initializers.First().Type
                                : typeof(object);

            return Expression.NewArrayInit(arrayType, initializers);
        }

        public override Expression VisitStackAllocArrayCreationExpression(StackAllocArrayCreationExpressionSyntax node)
        {
            return base.VisitStackAllocArrayCreationExpression(node);
        }

        public override Expression VisitOmittedArraySizeExpression(OmittedArraySizeExpressionSyntax node)
        {
            return base.VisitOmittedArraySizeExpression(node);
        }

        public override Expression VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node)
        {
            return base.VisitInterpolatedStringExpression(node);
        }

        public override Expression VisitInterpolatedStringText(InterpolatedStringTextSyntax node)
        {
            return base.VisitInterpolatedStringText(node);
        }

        public override Expression VisitInterpolation(InterpolationSyntax node)
        {
            return base.VisitInterpolation(node);
        }

        public override Expression VisitInterpolationAlignmentClause(InterpolationAlignmentClauseSyntax node)
        {
            return base.VisitInterpolationAlignmentClause(node);
        }

        public override Expression VisitInterpolationFormatClause(InterpolationFormatClauseSyntax node)
        {
            return base.VisitInterpolationFormatClause(node);
        }

        public override Expression VisitGlobalStatement(GlobalStatementSyntax node)
        {
            return base.VisitGlobalStatement(node);
        }

        public override Expression VisitBlock(BlockSyntax node)
        {
            var inBlockExpressions = new List<Expression>();
            var inBlockVariables = new List<ParameterExpression>();

            // с блоками есть определенные проблемы с возвращаемым значением.
            // если внутри блока есть возврат значения, необходимо создать 
            // метку выхода в верхнем блоке. 
            // проблемы начинаются если из блока возвращается например object

            // маркерный стек с помощью которого будем проверять, 
            // находится ли блок на верхнем уровне
            var marker = GetNamedStack<BlockSyntax>("BlockMarker");

            marker.Push(node);

            foreach (var statement in node.Statements)
            {
                var expression = Visit(statement);

                var wrapper = expression as VariableBlockWrapper;
                if (wrapper != null)
                {
                    inBlockExpressions.AddRange(wrapper.Expressions);
                    inBlockVariables.AddRange(wrapper.Variables);
                }
                else
                {
                    inBlockExpressions.Add(expression);
                }

                RegisterParam(expression);
            }

            marker.Pop();

            if (marker.Count == 0)
            {
                // если вышли в самый верхний блок, то проставляем метки
                // стек в котором будем хранить метки выхода с типами
                var @return = GetNamedQueue<LabelMarker>(BlockReturnQueue);
                while (@return.Count > 0)
                {
                    var target = @return.Dequeue();
                    inBlockExpressions.Add(Expression.Label(target.Target, Expression.Default(target.Type)));
                }
            }

            return Expression.Block(inBlockVariables, inBlockExpressions);
        }

        public override Expression VisitElementAccessExpression(ElementAccessExpressionSyntax node)
        {
            var expression = node.Expression.Accept(this);
            var argumentExpressions = new List<Expression>();
            foreach (var argument in node.ArgumentList.Arguments)
            {
                argumentExpressions.Add(argument.Accept(this));
            }

            return Expression.MakeIndex(expression, expression.Type.GetProperty("Item"), argumentExpressions);
        }

        public override Expression VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            //// пока не можем обработать объявление нескольких переменных вида
            //// var x = 1, y = 2
            //// только var x = 1

            Type variableType = null;
            var variable = node.Declaration.Variables.First();
            var typeInfo = _semanticModel.GetTypeInfo(node.Declaration.Type);

            if (typeInfo.Type != null)
            {
                variableType = ResolveType(typeInfo.Type, node);
            }
            else if (node.Declaration.Type.IsVar)
            {
                typeInfo = _semanticModel.GetTypeInfo(variable.Initializer.Value);
                variableType = ResolveType(typeInfo.Type, node);
            }

            var expression = Expression.Variable(variableType, variable.Identifier.Text);
            if (variable.Initializer != null)
            {
                var initializer = variable.Initializer.Value.Accept(this);
                var block = Expression.Block(new[] { expression }, Expression.Assign(expression, initializer));
                return new VariableBlockWrapper(block);
            }

            return expression;
        }

        public override Expression VisitVariableDeclaration(VariableDeclarationSyntax node)
        {
            Type variableType = null;
            var variable = node.Variables.First();
            var typeInfo = _semanticModel.GetTypeInfo(node.Type);

            if (typeInfo.Type != null)
            {
                variableType = ResolveType(typeInfo.Type, node);
            }
            else if (node.Type.IsVar)
            {
                typeInfo = _semanticModel.GetTypeInfo(variable.Initializer.Value);
                variableType = ResolveType(typeInfo.Type, node);
            }

            var expression = Expression.Variable(variableType, variable.Identifier.Text);
            if (variable.Initializer != null)
            {
                var initializer = variable.Initializer.Value.Accept(this);
                var block = Expression.Block(new[] { expression }, Expression.Assign(expression, initializer));
                return new VariableBlockWrapper(block);
            }

            return expression;
        }

        public override Expression VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            return base.VisitVariableDeclarator(node);
        }

        public override Expression VisitEqualsValueClause(EqualsValueClauseSyntax node)
        {
            return base.VisitEqualsValueClause(node);
        }

        public override Expression VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            return node.Expression.Accept(this);
        }

        public override Expression VisitEmptyStatement(EmptyStatementSyntax node)
        {
            return base.VisitEmptyStatement(node);
        }

        public override Expression VisitLabeledStatement(LabeledStatementSyntax node)
        {
            return base.VisitLabeledStatement(node);
        }

        public override Expression VisitGotoStatement(GotoStatementSyntax node)
        {
            return base.VisitGotoStatement(node);
        }

        public override Expression VisitBreakStatement(BreakStatementSyntax node)
        {
            var @break = GetNamedStack<LabelTarget>(LoopBreak).Peek();

            if (@break.Type != typeof(void))
            {
                return Expression.Break(@break, Expression.Default(@break.Type), @break.Type);
            }
            return Expression.Break(@break);
        }

        public override Expression VisitContinueStatement(ContinueStatementSyntax node)
        {
            var @continue = GetNamedStack<LabelTarget>(LoopContinue).Peek();
            return Expression.Continue(@continue);
        }

        public override Expression VisitReturnStatement(ReturnStatementSyntax node)
        {
            var expression = Visit(node.Expression);

            // создаем свою метку
            var labels = GetNamedQueue<LabelMarker>(BlockReturnQueue);
            var target = labels.FirstOrDefault(x => x.Type.IsAssignableFrom(expression.Type))?.Target;
            if (target == null)
            {
                var name = Guid.NewGuid().ToString("N");
                target = Expression.Label(expression.Type, name);
                labels.Enqueue(new LabelMarker { Name = name, Type = expression.Type, Target = target });
            }

            var @return = Expression.Return(target, expression, expression.Type);

            return @return;
        }

        public override Expression VisitUsingStatement(UsingStatementSyntax node)
        {
            return base.VisitUsingStatement(node);
        }

        public override Expression VisitFixedStatement(FixedStatementSyntax node)
        {
            return base.VisitFixedStatement(node);
        }

        public override Expression VisitCheckedStatement(CheckedStatementSyntax node)
        {
            return base.VisitCheckedStatement(node);
        }

        public override Expression VisitUnsafeStatement(UnsafeStatementSyntax node)
        {
            return base.VisitUnsafeStatement(node);
        }

        public override Expression VisitLockStatement(LockStatementSyntax node)
        {
            return base.VisitLockStatement(node);
        }

        public override Expression VisitCompilationUnit(CompilationUnitSyntax node)
        {
            return base.VisitCompilationUnit(node);
        }

        public override Expression VisitExternAliasDirective(ExternAliasDirectiveSyntax node)
        {
            return base.VisitExternAliasDirective(node);
        }

        public override Expression VisitUsingDirective(UsingDirectiveSyntax node)
        {
            return base.VisitUsingDirective(node);
        }

        public override Expression VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            return base.VisitNamespaceDeclaration(node);
        }

        public override Expression VisitArrayRankSpecifier(ArrayRankSpecifierSyntax node)
        {
            return base.VisitArrayRankSpecifier(node);
        }

        public override Expression VisitPointerType(PointerTypeSyntax node)
        {
            return base.VisitPointerType(node);
        }

        public override Expression VisitNullableType(NullableTypeSyntax node)
        {
            return base.VisitNullableType(node);
        }

        public override Expression VisitOmittedTypeArgument(OmittedTypeArgumentSyntax node)
        {
            return base.VisitOmittedTypeArgument(node);
        }

        public override Expression VisitArrayType(ArrayTypeSyntax node)
        {
            return base.VisitArrayType(node);
        }

        public override Expression VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            return base.VisitPropertyDeclaration(node);
        }

        public override Expression VisitArrowExpressionClause(ArrowExpressionClauseSyntax node)
        {
            return base.VisitArrowExpressionClause(node);
        }

        public override Expression VisitEventDeclaration(EventDeclarationSyntax node)
        {
            return base.VisitEventDeclaration(node);
        }

        public override Expression VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            var left = Visit(node.Left);
            var right = Visit(node.Right);

            return Expression.Assign(left, right);
        }

        public override Expression VisitThisExpression(ThisExpressionSyntax node)
        {
            return base.VisitThisExpression(node);
        }

        public override Expression VisitBaseExpression(BaseExpressionSyntax node)
        {
            return base.VisitBaseExpression(node);
        }

        public override Expression VisitAttribute(AttributeSyntax node)
        {
            return base.VisitAttribute(node);
        }

        public override Expression VisitAttributeArgument(AttributeArgumentSyntax node)
        {
            return base.VisitAttributeArgument(node);
        }

        public override Expression VisitNameEquals(NameEqualsSyntax node)
        {
            return base.VisitNameEquals(node);
        }

        public override Expression VisitTypeParameterList(TypeParameterListSyntax node)
        {
            return base.VisitTypeParameterList(node);
        }

        public override Expression VisitTypeParameter(TypeParameterSyntax node)
        {
            return base.VisitTypeParameter(node);
        }

        public override Expression VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            return base.VisitClassDeclaration(node);
        }

        public override Expression VisitStructDeclaration(StructDeclarationSyntax node)
        {
            return base.VisitStructDeclaration(node);
        }

        public override Expression VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            return base.VisitInterfaceDeclaration(node);
        }

        public override Expression VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            return base.VisitEnumDeclaration(node);
        }

        public override Expression VisitDelegateDeclaration(DelegateDeclarationSyntax node)
        {
            return base.VisitDelegateDeclaration(node);
        }

        public override Expression VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node)
        {
            return base.VisitEnumMemberDeclaration(node);
        }

        public override Expression VisitBaseList(BaseListSyntax node)
        {
            return base.VisitBaseList(node);
        }

        public override Expression VisitSimpleBaseType(SimpleBaseTypeSyntax node)
        {
            return base.VisitSimpleBaseType(node);
        }

        public override Expression VisitTypeParameterConstraintClause(TypeParameterConstraintClauseSyntax node)
        {
            return base.VisitTypeParameterConstraintClause(node);
        }

        public override Expression VisitConstructorConstraint(ConstructorConstraintSyntax node)
        {
            return base.VisitConstructorConstraint(node);
        }

        public override Expression VisitClassOrStructConstraint(ClassOrStructConstraintSyntax node)
        {
            return base.VisitClassOrStructConstraint(node);
        }

        public override Expression VisitTypeConstraint(TypeConstraintSyntax node)
        {
            return base.VisitTypeConstraint(node);
        }

        public override Expression VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            return base.VisitFieldDeclaration(node);
        }

        public override Expression VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
        {
            return base.VisitEventFieldDeclaration(node);
        }

        public override Expression VisitExplicitInterfaceSpecifier(ExplicitInterfaceSpecifierSyntax node)
        {
            return base.VisitExplicitInterfaceSpecifier(node);
        }

        public override Expression VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            return base.VisitMethodDeclaration(node);
        }

        public override Expression VisitOperatorDeclaration(OperatorDeclarationSyntax node)
        {
            return base.VisitOperatorDeclaration(node);
        }

        public override Expression VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node)
        {
            return base.VisitConversionOperatorDeclaration(node);
        }

        public override Expression VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            return base.VisitConstructorDeclaration(node);
        }

        public override Expression VisitConstructorInitializer(ConstructorInitializerSyntax node)
        {
            return base.VisitConstructorInitializer(node);
        }

        public override Expression VisitDestructorDeclaration(DestructorDeclarationSyntax node)
        {
            return base.VisitDestructorDeclaration(node);
        }

        public override Expression VisitAttributeArgumentList(AttributeArgumentListSyntax node)
        {
            return base.VisitAttributeArgumentList(node);
        }

        public override Expression VisitAttributeList(AttributeListSyntax node)
        {
            return base.VisitAttributeList(node);
        }

        public override Expression VisitAttributeTargetSpecifier(AttributeTargetSpecifierSyntax node)
        {
            return base.VisitAttributeTargetSpecifier(node);
        }

        public override Expression VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            var operand = node.Operand.Accept(this);
            switch (node.OperatorToken.Text)
            {
                case "-":
                    return Expression.Negate(operand);
                case "!":
                    return Expression.Not(operand);
            }

            return base.VisitPrefixUnaryExpression(node);
        }

        public override Expression VisitAwaitExpression(AwaitExpressionSyntax node)
        {
            return base.VisitAwaitExpression(node);
        }

        public override Expression VisitBadDirectiveTrivia(BadDirectiveTriviaSyntax node)
        {
            return base.VisitBadDirectiveTrivia(node);
        }

        public override Expression VisitDefineDirectiveTrivia(DefineDirectiveTriviaSyntax node)
        {
            return base.VisitDefineDirectiveTrivia(node);
        }

        public override Expression VisitUndefDirectiveTrivia(UndefDirectiveTriviaSyntax node)
        {
            return base.VisitUndefDirectiveTrivia(node);
        }

        public override Expression VisitLineDirectiveTrivia(LineDirectiveTriviaSyntax node)
        {
            return base.VisitLineDirectiveTrivia(node);
        }

        public override Expression VisitPragmaWarningDirectiveTrivia(PragmaWarningDirectiveTriviaSyntax node)
        {
            return base.VisitPragmaWarningDirectiveTrivia(node);
        }

        public override Expression VisitPragmaChecksumDirectiveTrivia(PragmaChecksumDirectiveTriviaSyntax node)
        {
            return base.VisitPragmaChecksumDirectiveTrivia(node);
        }

        public override Expression VisitReferenceDirectiveTrivia(ReferenceDirectiveTriviaSyntax node)
        {
            return base.VisitReferenceDirectiveTrivia(node);
        }

        public override Expression VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            var nameInfo = _semanticModel.GetSymbolInfo(node.Name);
            var symbol = nameInfo.Symbol;

            var field = symbol as IFieldSymbol;
            if (field != null && field.IsStatic)
            {
                var type = ResolveType(field.ContainingType);
                var typeField = type.GetField(field.Name);
                if (typeField.IsStatic)
                {
                    return Expression.Field(null, typeField);
                }
            }

            var expression = node.Expression.Accept(this);

            if (symbol == null)
            {
                if (nameInfo.CandidateSymbols.Length == 1 && nameInfo.CandidateReason == CandidateReason.NotAValue)
                {
                    symbol = nameInfo.CandidateSymbols[0];
                }
                else
                {
                    // бывает что динамически резолвленные типы не известны 
                    // в _semanticModel (возможно стоит ее перестраивать при нахождении нового типа)
                    if (node.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                    {
                        // пытаемся найти известный член в типе старшего выражения
                        var member = expression.Type.GetMember(node.Name.Identifier.Text).FirstOrDefault();
                        if (member != null)
                        {
                            if (member is MethodBase)
                            {
                                // обработка на метод
                                throw new NotImplementedException();
                            }
                            return Expression.MakeMemberAccess(expression, member);
                        }
                    }

                    throw new Exception(string.Format("Свойство {0} не найдено", node.Name.Identifier.Text));
                }
            }

            switch (symbol.Kind)
            {
                case SymbolKind.Property:
                    return Expression.Property(expression, node.Name.Identifier.Text);
            }

            return base.VisitMemberAccessExpression(node);
        }

        public override Expression VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
        {
            return base.VisitConditionalAccessExpression(node);
        }

        public override Expression VisitMemberBindingExpression(MemberBindingExpressionSyntax node)
        {
            return base.VisitMemberBindingExpression(node);
        }

        public override Expression VisitElementBindingExpression(ElementBindingExpressionSyntax node)
        {
            return base.VisitElementBindingExpression(node);
        }

        public override Expression VisitImplicitElementAccess(ImplicitElementAccessSyntax node)
        {
            return base.VisitImplicitElementAccess(node);
        }

        public override Expression VisitIdentifierName(IdentifierNameSyntax node)
        {
            var parameters = GetNamedStack<ParameterExpression>(ParameterExpressions);
            var result = parameters.SingleOrDefault(x => x.Name == node.Identifier.ValueText);
            if (result != null)
            {
                return result;
            }

            if (_parent != null)
            {
                return _parent.VisitIdentifierName(node);
            }

            throw new Exception($"Переменная {node.Identifier.ValueText} не найдена");
        }

        public override Expression VisitQualifiedName(QualifiedNameSyntax node)
        {
            // обрабатываем тип
            var name = node.ToFullString();
            var symbol = _semanticModel.Compilation.GetTypeByMetadataName(name);
            var type = ResolveType(symbol);
            if (type != null)
                return Expression.Constant(type);

            return base.VisitQualifiedName(node);
        }

        public override Expression VisitGenericName(GenericNameSyntax node)
        {
            return base.VisitGenericName(node);
        }

        public override Expression VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
        {
            var methodInfo = (IMethodSymbol)_semanticModel.GetSymbolInfo(node).Symbol;
            var lambdaParams = new[] { Expression.Parameter(ResolveType(methodInfo.Parameters[0].Type, node), node.Parameter.Identifier.Text) };

            var context = new ExpressionSyntaxVisitor(this, _semanticModel, lambdaParams, _resolutionService);
            return Expression.Lambda(node.Body.Accept(context), lambdaParams);
        }

        public override Expression VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
        {
            return base.VisitParenthesizedLambdaExpression(node);
        }

        public override Expression VisitInitializerExpression(InitializerExpressionSyntax node)
        {
            return base.VisitInitializerExpression(node);
        }

        public override Expression VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            var left = node.Left.Accept(this);
            var right = node.Right.Accept(this);
            
            if (left.Type == typeof(string) && node.OperatorToken.Text == "+")
            {
                var objectType = typeof(object);
                var method = typeof(string).GetMethod("Concat", new[] { objectType, objectType });
                return Expression.Call(null, method, Expression.Convert(left, objectType), Expression.Convert(right, objectType));
            }
            
            switch (node.Kind())
            {                    
                case SyntaxKind.AsExpression:
                    var asConstant = (ConstantExpression)right;
                    return Expression.TypeAs(left, (Type)asConstant.Value);
                case SyntaxKind.IsExpression:
                    var isConstant = (ConstantExpression)right;
                    return Expression.TypeIs(left, (Type)isConstant.Value);                    
                    
            }

            if (!left.Type.IsAssignableFrom(right.Type))
            {
                right = Expression.Convert(right, left.Type);
            }

            switch (node.OperatorToken.Text)
            {
                case "+":
                    return Expression.Add(left, right);

                case "+=":
                    return Expression.AddAssign(left, right);

                case "-":
                    return Expression.Subtract(left, right);

                case "-=":
                    return Expression.SubtractAssign(left, right);

                case ">=":
                    return Expression.GreaterThanOrEqual(left, right);

                case ">":
                    return Expression.GreaterThan(left, right);

                case "<=":
                    return Expression.LessThanOrEqual(left, right);

                case "<":
                    return Expression.LessThan(left, right);

                case "==":
                    return Expression.Equal(left, right);

                case "!=":
                    return Expression.NotEqual(left, right);

                case "||":
                    return Expression.OrElse(left, right);

                case "&&":
                    return Expression.AndAlso(left, right);

                case "*":
                    return Expression.Multiply(left, right);

                case "*=":
                    return Expression.MultiplyAssign(left, right);

                case "??":
                    return Expression.Coalesce(left, right);

                case "%":
                    return Expression.Modulo(left, right);                                        
            }

            throw new NotImplementedException();
        }

        public override Expression VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            return Expression.Constant(node.Token.Value);
        }

        public override Expression VisitMakeRefExpression(MakeRefExpressionSyntax node)
        {
            return base.VisitMakeRefExpression(node);
        }

        public override Expression VisitRefTypeExpression(RefTypeExpressionSyntax node)
        {
            return base.VisitRefTypeExpression(node);
        }

        public override Expression VisitRefValueExpression(RefValueExpressionSyntax node)
        {
            return base.VisitRefValueExpression(node);
        }

        public override Expression VisitCheckedExpression(CheckedExpressionSyntax node)
        {
            return base.VisitCheckedExpression(node);
        }

        public override Expression VisitDefaultExpression(DefaultExpressionSyntax node)
        {
            return base.VisitDefaultExpression(node);
        }

        public override Expression VisitTypeOfExpression(TypeOfExpressionSyntax node)
        {
            if (node.Type.IsKind(SyntaxKind.PredefinedType))
            {
                return node.Type.Accept(this);
            }

            if (node.Type.IsKind(SyntaxKind.QualifiedName))
            {
                var symbol = _semanticModel.Compilation.GetTypeByMetadataName(node.Type.ToFullString());
                var type = ResolveType(symbol);                
                return Expression.Constant(type);
            }

            return base.VisitTypeOfExpression(node);
        }

        public override Expression VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
        {
            return node.Expression.Accept(this);
        }

        public override Expression VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            var typeInfo = _semanticModel.GetTypeInfo(node);
            var type = ResolveType(typeInfo.Type, node);

            NewExpression @new;
            if (node.ArgumentList == null)
            {
                @new = Expression.New(type);
            }
            else
            {
                var arguments = node.ArgumentList.Arguments.Select(x => x.Accept(this)).ToArray();
                var argumentTypes = arguments.Select(x => x.Type).ToArray();

                if (type.IsGenericType)
                {
                    var genericDefinition = type.GetGenericTypeDefinition();
                    var lambda = arguments.Length > 0
                                     ? arguments[0] as LambdaExpression
                                     : null;
                    if (lambda != null && typeof(Delegate).IsAssignableFrom(genericDefinition))
                    {
                        var lambdaMethod = typeof(Expression).GetMethods().First(x => x.IsPublic && x.IsStatic && x.IsGenericMethodDefinition);
                        var delegateTypeArguments = lambda.Parameters.Select(x => x.Type).ToList();
                        if (type.GenericTypeArguments.Length == delegateTypeArguments.Count + 1)
                        {
                            delegateTypeArguments.Add(lambda.ReturnType);
                        }

                        var delegateType = genericDefinition.MakeGenericType(delegateTypeArguments.ToArray());
                        var delegateLambda = lambdaMethod.MakeGenericMethod(delegateType);
                        var expression = delegateLambda.Invoke(null, new object[] { lambda.Body, lambda.Parameters.ToArray() }) as LambdaExpression;
                        return expression;
                    }
                }

                //// по хорошему нужно заменить эти развесистые if на отдельные сервисы

                var ctor = type.GetConstructor(argumentTypes);
                @new = Expression.New(ctor, arguments);
            }

            if (node.Initializer != null)
            {
                if (typeof(ICollection).IsAssignableFrom(type))
                {
                    // например new List<int>{ 1, 2, 3 }                    
                    var initializers = node.Initializer.Expressions.Select(Visit).ToArray();
                    var listInit = Expression.ListInit(@new, initializers);
                    return listInit;
                }

                var bindings = new List<MemberBinding>();
                foreach (var expression in node.Initializer.Expressions)
                {
                    if (expression is AssignmentExpressionSyntax)
                    {
                        bindings.Add(VisitMemberInitExpression(type, (AssignmentExpressionSyntax)expression));
                    }
                    else if (expression is MemberAccessExpressionSyntax)
                    {
                        var propertyName = (expression as MemberAccessExpressionSyntax).Name.Identifier.Text;
                        var left = Expression.Property(@new, propertyName);
                        var right = VisitMemberAccessExpression(expression as MemberAccessExpressionSyntax);

                        if (!left.Type.IsAssignableFrom(right.Type))
                        {
                            right = Expression.Convert(right, left.Type);
                        }

                        bindings.Add(Expression.Bind(left.Member, right));
                    }
                    else
                    {
                        var value = expression.Accept(this);
                        throw new NotImplementedException();
                    }
                }

                return Expression.MemberInit(@new, bindings);
            }

            return @new;
        }

        public override Expression VisitCastExpression(CastExpressionSyntax node)
        {
            var typeInfo = _semanticModel.GetTypeInfo(node);
            return Expression.Convert(node.Expression.Accept(this), ResolveType(typeInfo.Type, node));
        }

        public override Expression VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            var operand = node.Operand.Accept(this);

            switch (node.Kind())
            {
                case SyntaxKind.PostDecrementExpression:
                    return Expression.PostDecrementAssign(operand);

                case SyntaxKind.PreDecrementExpression:
                    return Expression.PreDecrementAssign(operand);

                case SyntaxKind.PostIncrementExpression:
                    return Expression.PostIncrementAssign(operand);

                case SyntaxKind.PreIncrementExpression:
                    return Expression.PreIncrementAssign(operand);

                default:
                    throw new NotImplementedException();
            }
        }

        public override Expression DefaultVisit(SyntaxNode node)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        private MemberBinding VisitMemberInitExpression(Type ownerType, AssignmentExpressionSyntax expression)
        {
            if (expression.Left is IdentifierNameSyntax)
            {
                var target = (IdentifierNameSyntax)expression.Left;

                if (expression.Right is InitializerExpressionSyntax)
                {
                    var property = ownerType.GetProperty(target.Identifier.Text);

                    return Expression.ListBind(property, ((InitializerExpressionSyntax)expression.Right).Expressions.Select(x => VisitElementInit(property.PropertyType, x)));
                }

                return Expression.Bind(ownerType.GetProperty(target.Identifier.Text), expression.Right.Accept(this));
            }

            throw new NotImplementedException();
        }

        private ElementInit VisitElementInit(Type ownerType, ExpressionSyntax expression)
        {
            return Expression.ElementInit(ownerType.GetMethod("Add"), expression.Accept(this));
        }

        private bool CheckMethodArguments(ParameterInfo[] @params, Type[] parameterTypes)
        {
            if (@params.Length < parameterTypes.Length)
            {
                return false;
            }

            if (parameterTypes.Where((t, idx) => !@params[idx].ParameterType.IsAssignableFrom(t)).Any())
            {
                return false;
            }

            if (@params.Length > parameterTypes.Length)
            {
                return @params.Skip(parameterTypes.Length).All(x => x.IsOptional);
            }

            return true;
        }

        private MethodInfo ResolveMethod(IMethodSymbol symbol, Type[] argumentTypes)
        {
            var type = ResolveType(symbol.ContainingType);

            var methods = type.GetMethods().Where(x => x.Name == symbol.Name).ToArray();

            foreach (var method in methods)
            {
                if (method.IsGenericMethod)
                {
                    if (!symbol.IsGenericMethod)
                    {
                        continue;
                    }

                    if (symbol.TypeArguments.Length != method.GetGenericArguments().Length)
                    {
                        continue;
                    }

                    var typeArguments = symbol.TypeArguments.Select(typeSymbol => ResolveType(typeSymbol)).ToArray();
                    var realMethod = method.MakeGenericMethod(typeArguments);

                    if (realMethod.ReturnType != ResolveType(symbol.ReturnType))
                    {
                        continue;
                    }

                    if (CheckMethodArguments(realMethod.GetParameters(), argumentTypes))
                    {
                        return realMethod;
                    }
                }
                else if (CheckMethodArguments(method.GetParameters(), argumentTypes))
                {
                    return method;
                }
            }

            throw new NotImplementedException();
        }

        private Type ResolveType(ITypeSymbol typeSymbol, SyntaxNode node = null)
        {
            return _resolutionService.Resolve(typeSymbol);
        }

        private TypeInfo GetTypeInfo(SyntaxNode node)
        {
            var typeInfo = _semanticModel.GetTypeInfo(node);
            if (typeInfo.Type == null)
            {
                typeInfo = _semanticModel.GetSpeculativeTypeInfo(node.FullSpan.Start, node, SpeculativeBindingOption.BindAsTypeOrNamespace);
            }

            if (typeInfo.Type == null)
            {
                var expressionProperty = node.GetType().GetProperty("Expression");
                if (expressionProperty != null && typeof(ExpressionSyntax).IsAssignableFrom(expressionProperty.PropertyType))
                {
                    var expression = expressionProperty.GetValue(node) as ExpressionSyntax;
                    typeInfo = GetTypeInfo(expression);
                }
            }

            return typeInfo;
        }
    }
}