namespace Expresso.Resolvers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Fasterflect;
    using Microsoft.CodeAnalysis;

    /// <summary>
    /// Разрешитель обобщенного типа
    /// </summary>
    public class GenericTypeResolver : TypeResolver
    {
        /// <summary>
        /// Разрешить обобщённый тип
        /// </summary>
        /// <param name="service">Сервис разрешения типа при парсинге строк в выражения</param>
        /// <returns></returns>
        public override Type Resolve(TypeResolutionService service)
        {
            var named = (INamedTypeSymbol)service.Symbol;
            var genericType = named.ConstructUnboundGenericType();
            var unboundType = service.Resolve(genericType);
            var typeArguments = (IEnumerable<ITypeSymbol>)named.GetPropertyValue("TypeArguments");
            // по какой то причине named.TypeArguments выходит с ошибкой
            
            var arguments = typeArguments.Select(service.Resolve).ToArray();

            if (arguments.Length != named.Arity)
            {
                throw new Exception("Не удалось разрешить все типы аргументов обобщенного типа");
            }

            return unboundType.MakeGenericType(arguments);
        }

        /// <summary>
        /// Определение возможноси разрешить тип
        /// </summary>
        /// <param name="service">Контекст разрешения типа</param>
        /// <returns></returns>
        public override bool CanResolve(TypeResolutionService service)
        {
            var named = service.Symbol as INamedTypeSymbol;
            return named != null && named.Arity > 0 && named.IsGenericType && !named.IsUnboundGenericType;
        }
    }
}