namespace Expresso.ExpressionToCode {
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using Fasterflect;

    internal class AnonymousObjectProcessor : ConstantExpressionProcessor
    {
        public AnonymousObjectProcessor(ExpressionWalker walker)
            : base(walker) { }

        public override bool CanProcess(ConstantExpression expression, object value)
        {
            if (value == null)
                return false;

            var type = value.GetType();
            return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
                   && type.IsGenericType && type.Name.Contains("AnonymousType")
                   && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                   && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }

        public override string Process(ConstantExpression expression, object value)
        {
            // получаем параметры конструктора (который у анонимного типа ровно один)
            var ctorArgs = value.GetType().GetConstructors()[0].GetParameters();
            var initializer = new List<string>();
            foreach (var arg in ctorArgs)
            {
                var argVal = value.GetPropertyValue(arg.Name);
                var expr = Expression.Constant(argVal);
                var exprStr = Walker.Visit(expr).Key;
                initializer.Add($"{arg.Name} = {exprStr}");
            }

            return $"new {{ {string.Join(",", initializer)} }}";
        }
    }
}