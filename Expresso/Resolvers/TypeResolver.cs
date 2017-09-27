namespace Expresso.Resolvers
{
    using System;

    /// <summary>
    /// Абстрактный класс разрешителя типов, применяемый при парсинге строковых выражений с помощью Roslyn
    /// </summary>
    public abstract class TypeResolver
    {
        /// <summary>
        /// Разрешение типа на основе его описания в синтаксическом дереве
        /// </summary>
        /// <param name="service">Контекст разрешения типа</param>
        public abstract Type Resolve(TypeResolutionService service);

        /// <summary>
        /// Определение возможноси разрешить тип
        /// </summary>
        /// <param name="service">Контекст разрешения типа</param>
        /// <returns></returns>
        public abstract bool CanResolve(TypeResolutionService service);
    }
}