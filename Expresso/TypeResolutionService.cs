namespace Expresso
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Expresso.Resolvers;
    using Expresso.Utils;
    using Microsoft.CodeAnalysis;

    /// <summary>
    /// Сервис разрешения типа при парсинге строк в выражения
    /// </summary>
    public class TypeResolutionService
    {
        private readonly Stack<ITypeSymbol> _resolutionStack = new Stack<ITypeSymbol>();

        private readonly Stack<TypeResolver> _resolvers = new Stack<TypeResolver>();

        /// <summary>
        /// Текущий обрабатываемый тип
        /// </summary>
        public ITypeSymbol Symbol => _resolutionStack.FirstOrDefault();

        /// <summary>
        /// Добавить разрешитель типов
        /// </summary>
        /// <param name="resolver">Экземпляр разрешителя типов</param>        
        public TypeResolutionService Add(TypeResolver resolver)
        {
            ArgumentChecker.NotNull(resolver, nameof(resolver));

            _resolvers.Push(resolver);
            return this;
        }

        /// <summary>
        /// Добавить разрешитель типов
        /// </summary>
        /// <typeparam name="T">Тип разрешителя</typeparam>        
        public TypeResolutionService Add<T>()
            where T : TypeResolver, new()
        {
            return Add(new T());
        }
        
        /// <summary>
        /// Разрешить тип по его описанию
        /// </summary>
        /// <param name="symbol">Контекст разрешения типа</param>
        /// <returns></returns>
        public Type Resolve(ITypeSymbol symbol)
        {            
            if (_resolutionStack.Contains(symbol))
            {
                // если опять пришли к типу, который уже находится в контексте разрешения
                // то скорее всего мы столкнулись с бесконечным циклом
                throw new InvalidOperationException("Возможно попали в бесконечный цикл при разрешении типа");
            }

            _resolutionStack.Push(symbol);

            var type = _resolvers
                .Where(x => x.CanResolve(this))
                .Select(x => x.Resolve(this))
                .FirstOrDefault(x => x != null);

            if (type == null)
            {
                throw new InvalidOperationException("Не удалось разрешить тип " + symbol.Name);
            }

            _resolutionStack.Pop();

            return type;
        }
    }
}