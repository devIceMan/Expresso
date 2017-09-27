namespace Expresso.Resolvers
{
    using System;
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;

    /// <summary>
    /// Разрешитель анонимных типов.
    /// В случае отсутствия анонимного типа, он будет создан
    /// </summary>
    public class AnonymousTypeResolver : TypeResolver
    {
        /// <summary>
        /// Разрешение типа на основе его описания в синтаксическом дереве
        /// </summary>
        /// <param name="service">Контекст разрешения типа</param>
        public override Type Resolve(TypeResolutionService service)
        {
            var properties = new Dictionary<string, Type>();
            foreach (var member in service.Symbol.GetMembers())
            {
                var property = member as IPropertySymbol;
                if (property == null)
                {
                    continue;
                }

                var propertyType = service.Resolve(property.Type);
                var propertyName = property.Name;

                properties[propertyName] = propertyType;
            }

            return AnonymousTypeBuilder.Build(properties);
        }

        /// <summary>
        /// Определение возможноси разрешить тип
        /// </summary>
        /// <param name="service">Контекст разрешения типа</param>
        /// <returns></returns>
        public override bool CanResolve(TypeResolutionService service)
        {
            return service.Symbol.IsAnonymousType;
        }
    }
}