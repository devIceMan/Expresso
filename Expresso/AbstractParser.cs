namespace Expresso
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Expresso.Resolvers;
    using Expresso.Utils;
    using JetBrains.Annotations;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Emit;

    /// <summary>
    /// Абстрактный класс компилятора
    /// </summary>
    /// <typeparam name="TImplementation"></typeparam>
    public abstract class AbstractCompiler<TImplementation> : AbstractParser<TImplementation>
        where TImplementation : AbstractCompiler<TImplementation>
    {
        /// <summary>
        /// Скомпилировать код в сборку
        /// </summary>
        /// <param name="code"></param>
        /// <param name="assemblyName"></param>
        /// <param name="useTypes"></param>
        /// <returns></returns>
        public virtual Assembly CompileToAssembly(string code, string assemblyName = "AbstractCompilerAssembly", Type[] useTypes = null)
        {
            var tree = GetSyntaxTree(code);
            var compilation = GetCompilation(tree, assemblyName, useTypes);

            using (var ms = new MemoryStream())
            {
                var compilationResult = compilation.Emit(ms);

                if (!compilationResult.Success)
                    throw new RoslynCompilationException("Assembly could not be compiled", compilationResult);

                ms.Seek(0, SeekOrigin.Begin);
                return Assembly.Load(ms.ToArray());
            }
        }

        /// <summary>
        /// Собрать код в сборку и вернуть тип
        /// </summary>
        /// <param name="code"></param>
        /// <param name="className"></param>
        /// <param name="assemblyName"></param>
        /// <param name="useTypes"></param>
        /// <returns></returns>
        public Type CompileToType(string code, string className = null, string assemblyName = null, Type[] useTypes = null)
        {
            assemblyName = assemblyName ?? $"AbstractParserAsm{Guid.NewGuid().ToString("N").ToUpper()}";
            var assembly = CompileToAssembly(code, assemblyName, useTypes);
            return className == null ? assembly.GetTypes().First() : assembly.GetType(className);
        }

        /// <summary>
        /// Собрать код и вернуть инстанс
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="code"></param>
        /// <param name="ctorParams"></param>
        /// <param name="className"></param>
        /// <param name="assemblyName"></param>
        /// <param name="useTypes"></param>
        /// <returns></returns>
        public T CompileToInstance<T>(string code, object[] ctorParams = null, string className = null, string assemblyName = "AbstractCompilerAssembly", Type[] useTypes = null)
        {
            var type = CompileToType(code, className, assemblyName, useTypes);
            return ctorParams == null ? (T) Activator.CreateInstance(type) : (T) Activator.CreateInstance(type, ctorParams);
        }

        /// <inheritdoc />
        protected override CSharpCompilationOptions GetCompilationOptions()
        {
            var options = base.GetCompilationOptions();
            return options
                .WithReportSuppressedDiagnostics(true)
                .WithOptimizationLevel(OptimizationLevel.Debug)
                .WithGeneralDiagnosticOption(ReportDiagnostic.Error);
        }
    }

    /// <summary>
    /// Исключительная ситуация при компиляции
    /// </summary>
    public class RoslynCompilationException : Exception
    {
        /// <summary>
        /// Результат компиляции
        /// </summary>
        public EmitResult Result { get; }

        /// <summary>
        /// Создание нового экземпляра
        /// </summary>
        /// <param name="message"></param>
        /// <param name="result"></param>
        public RoslynCompilationException(string message, EmitResult result)
            : base(message)
        {
            Result = result;
        }

        /// <summary>
        /// Создание нового экземпляра
        /// </summary>
        /// <param name="result"></param>
        public RoslynCompilationException(EmitResult result)
            : this("Could not compile :" + string.Join(Environment.NewLine, result.Diagnostics.Select(x => x.ToString())), result)
        {

        }
    }

    public abstract class AbstractParser<TImplementation>
        where TImplementation : AbstractParser<TImplementation>
    {
        private readonly IDictionary<string, string> _aliases = new Dictionary<string, string>(StringComparer.Ordinal);

        private readonly List<IAssemblyProvider> _assemblyProviders = new List<IAssemblyProvider>();
        private readonly HashSet<Assembly> _references = new HashSet<Assembly>();

        private readonly IList<CSharpSyntaxRewriter> _rewriters = new List<CSharpSyntaxRewriter>();

        private readonly HashSet<string> _usings = new HashSet<string>(StringComparer.Ordinal);

        protected readonly TypeResolutionService ResolutionService = new TypeResolutionService();

        protected AbstractParser()
        {
            // регистрируем пространства имен по-умолчанию
            UsingNamespace("System")
                .UsingNamespace("System.Linq")
                .UsingNamespace("System.Linq.Expressions");

            // регистрируем разрешители по-умолчанию
            UsingTypeResolver<AppDomainTypeResolver>()
                .UsingTypeResolver<AnonymousTypeResolver>()
                .UsingTypeResolver<ArrayTypeResolver>()
                .UsingTypeResolver<GenericTypeResolver>();
        }

        /// <summary>
        /// Регистрация ссылки на сборку
        /// </summary>
        /// <param name="assembly">Сборка</param>
        public TImplementation WithAssembly([NotNull] Assembly assembly)
        {
            ArgumentChecker.NotNull(assembly, nameof(assembly));

            _references.Add(assembly);

            return (TImplementation) this;
        }

        /// <summary>
        /// Регистрация ссылки на сборку, в которой определен тип <see cref="type" />
        /// </summary>
        /// <param name="type">Тип, определенный в сборке</param>
        public TImplementation WithAssemblyOf([NotNull] Type type)
        {
            ArgumentChecker.NotNull(type, nameof(type));

            return WithAssembly(type.Assembly);
        }

        /// <summary>
        /// Регистрация ссылки на сборку, в которой определен тип <see cref="T" />
        /// </summary>
        /// <typeparam name="T">Тип, определенный в сборке</typeparam>
        public TImplementation WithAssemblyOf<T>()
        {
            return WithAssemblyOf(typeof(T));
        }

        /// <summary>
        /// Добавить используемое пространство имен.
        /// Это позволяет не указывать полное имя типа в выражении
        /// </summary>
        /// <param name="namespace">Постранство имен</param>
        public TImplementation UsingNamespace(params string[] @namespace)
        {
            foreach (var ns in @namespace)
                _usings.Add(ns);

            return (TImplementation) this;
        }

        /// <summary>
        /// Добавить пространство имен типа
        /// </summary>
        /// <param name="types">Типы, пространсто имен которых необходимо добавить</param>
        public TImplementation UsingNamespaceOf([NotNull] params Type[] types)
        {
            ArgumentChecker.NotNull(types, nameof(types));

            var namespaces = types.Where(x => x != null).Select(x => x.Namespace).Distinct(StringComparer.Ordinal).ToArray();
            foreach (var ns in namespaces)
            {
                UsingNamespace(ns);
            }

            return (TImplementation) this;
        }

        /// <summary>
        /// Добавить пространство имен типа
        /// </summary>
        /// <typeparam name="T">Тип, пространсто имен которого необходимо добавить</typeparam>
        public TImplementation UsingNamespaceOf<T>()
        {
            return UsingNamespace(typeof(T).Namespace);
        }

        /// <summary>
        /// Добавить ссылку на сборку содержащую тип а также пространство имен
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public TImplementation Using(Type type)
        {
            return WithAssemblyOf(type).UsingNamespaceOf(type);
        }

        /// <summary>
        /// Добавить ссылку на сборку содержащую тип а также пространство имен
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public TImplementation Using<T>()
        {
            return Using(typeof(T));
        }

        /// <summary>
        /// Добавить псевдоним типа.
        /// Позволяет сократить длинные имена типов в выражениях.
        /// Будет использован в виде <c>using Alias = Ns1.Ns2.Ns3.Type</c>
        /// </summary>
        /// <param name="alias">Псевдоним типа</param>
        /// <param name="typeFullName">Полное имя типа</param>
        /// <returns></returns>
        public TImplementation UsingTypeAlias(string alias, string typeFullName)
        {
            ArgumentChecker.NotEmpty(alias, nameof(alias));
            ArgumentChecker.NotEmpty(typeFullName, nameof(typeFullName));

            _aliases.Add(alias, typeFullName);

            return (TImplementation) this;
        }

        /// <summary>
        /// Добавить псевдоним типа.
        /// Позволяет сократить длинные имена типов в выражениях.
        /// Будет использован в виде <c>using Alias = Ns1.Ns2.Ns3.Type</c>
        /// </summary>
        /// <param name="alias">Псевдоним</param>
        /// <param name="type">Тип</param>
        public TImplementation UsingTypeAlias(string alias, Type type)
        {
            ArgumentChecker.NotEmpty(alias, nameof(alias));
            ArgumentChecker.NotNull(type, nameof(type));

            return UsingTypeAlias(alias, TypeUtils.CSharpTypeName(type));
        }

        /// <summary>
        /// Добавить псевдоним типа
        /// Позволяет сократить длинные имена типов в выражениях.
        /// Будет использован в виде <c>using Alias = Ns1.Ns2.Ns3.Type</c>
        /// </summary>
        /// <typeparam name="T">Тип</typeparam>
        /// <param name="alias">Псевдоним</param>
        public TImplementation UsingTypeAlias<T>(string alias)
        {
            ArgumentChecker.NotEmpty(alias, nameof(alias));

            return UsingTypeAlias(alias, typeof(T));
        }

        /// <summary>
        /// Использовать обработчик (<see cref="CSharpSyntaxRewriter" />) синтаксического дерева
        /// </summary>
        /// <param name="rewriter">Экземпляр обработчика</param>
        public TImplementation UsingRewriter(CSharpSyntaxRewriter rewriter)
        {
            ArgumentChecker.NotNull(rewriter, nameof(rewriter));

            _rewriters.Add(rewriter);

            return (TImplementation) this;
        }

        /// <summary>
        /// Использовать переданный разрешитель типов
        /// </summary>
        /// <param name="resolver">Экземпляр разрешителя типов</param>
        /// <returns></returns>
        public TImplementation UsingTypeResolver(TypeResolver resolver)
        {
            ArgumentChecker.NotNull(resolver, nameof(resolver));

            ResolutionService.Add(resolver);

            return (TImplementation) this;
        }

        /// <summary>
        /// Использовать разрегитель типов типа <see cref="T" />
        /// </summary>
        /// <typeparam name="T">Тип разрешителя</typeparam>
        /// <returns></returns>
        public TImplementation UsingTypeResolver<T>()
            where T : TypeResolver, new()
        {
            return UsingTypeResolver(new T());
        }

        /// <summary>
        /// Использовать поставщик сборок
        /// </summary>
        /// <param name="provider">Экземпляр поставщика сборок</param>
        public TImplementation WithAssemblyProvider([NotNull] IAssemblyProvider provider)
        {
            ArgumentChecker.NotNull(provider, nameof(provider));

            _assemblyProviders.Add(provider);

            return (TImplementation) this;
        }

        /// <summary>
        /// Использовать поставщик сборок типа <see cref="type" />
        /// </summary>
        /// <param name="type">Тип поставщика сборок</param>
        public TImplementation WithAssemblyProvider([NotNull] Type type)
        {
            ArgumentChecker.NotNullAndIs<IAssemblyProvider>(type, nameof(type));

            var provider = (IAssemblyProvider) Activator.CreateInstance(type);
            return WithAssemblyProvider(provider);
        }

        /// <summary>
        /// Использовать поставщик сборок типа <see cref="T" />
        /// </summary>
        /// <typeparam name="T">Тип поставщика сборок</typeparam>
        /// <returns></returns>
        public TImplementation WithAssemblyProvider<T>()
            where T : IAssemblyProvider, new()
        {
            return WithAssemblyProvider(new T());
        }

        /// <summary>
        /// Получить синтаксическое дерево для кода
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        protected SyntaxTree GetSyntaxTree(string text)
        {
            var sb = new StringBuilder();
            foreach (var @using in _usings)
                sb.AppendLine($"using {@using};");

            foreach (var alias in _aliases)
                sb.AppendLine($"using {alias.Key} = {alias.Value};");

            sb.AppendLine(text);

            var code = sb.ToString();
            var tree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(LanguageVersion.CSharp6, DocumentationMode.None, SourceCodeKind.Regular));
            var root = _rewriters.Aggregate(tree.GetRoot(), (current, rewriter) => rewriter.Visit(current));

            return CSharpSyntaxTree.Create((CSharpSyntaxNode) root);
        }

        /// <summary>
        /// Собрать используемые в коде сборки
        /// </summary>
        /// <returns></returns>
        protected virtual Assembly[] CollectAssemblies()
        {
            var assemblies = new List<Assembly>(_references);
            var providedAssemblies = _assemblyProviders.SelectMany(x => x.GetAssemblies());
            assemblies.AddRange(providedAssemblies);

            CollectAssembliesFromTypes(new[] {
                typeof(Enumerable),
                typeof(ICollection<>),
                typeof(IQueryable<>),
                typeof(DateTime),
                typeof(Predicate<>)
            }, assemblies);

            return assemblies.ToArray();
        }

        /// <summary>
        /// Создание опций компиляции
        /// </summary>
        /// <returns></returns>
        [NotNull]
        protected virtual CSharpCompilationOptions GetCompilationOptions()
        {
            return new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        }

        /// <summary>
        /// Получить компилятор
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="assemblyName"></param>
        /// <param name="useTypes"></param>
        /// <returns></returns>
        protected virtual CSharpCompilation GetCompilation(SyntaxTree tree, string assemblyName = "AbstractParserAssembly", Type[] useTypes = null)
        {
            var assemblies = CollectAssemblies().ToList();
            CollectAssembliesFromTypes(useTypes ?? Enumerable.Empty<Type>(), assemblies);
            assemblies = assemblies
                .Where(x => !x.IsDynamic)
                .Distinct(AssemblyNameComparer.Instance)
                .ToList();

            return CSharpCompilation.Create(assemblyName, options: GetCompilationOptions())
                .AddSyntaxTrees(tree)
                .AddReferences(CollectReferences(assemblies));
        }

        protected static void CollectAssembliesFromTypes(IEnumerable<Type> types, List<Assembly> assemblies)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var dictionary = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
            foreach (var type in types)
                CollectAssembliesFromTypes(type, dictionary, set);

            assemblies.AddRange(dictionary.Values);
        }

        /// <summary>
        /// Собрать используемые сборки по указанному типу
        /// </summary>
        /// <param name="type"></param>
        /// <param name="assemblies"></param>
        /// <param name="processed"></param>
        private static void CollectAssembliesFromTypes(Type type, IDictionary<string, Assembly> assemblies, HashSet<string> processed)
        {
            if (processed.Contains(type.FullName))
                return;

            var assemblyName = type.Assembly.GetName();
            if (!assemblies.ContainsKey(assemblyName.Name))
                assemblies.Add(assemblyName.Name, type.Assembly);

            processed.Add(type.FullName);

            foreach (var member in type.GetMembers())
            {
                var property = member as PropertyInfo;
                if (property != null)
                    CollectAssembliesFromTypes(property.PropertyType, assemblies, processed);

                var field = member as FieldInfo;
                if (field != null)
                    CollectAssembliesFromTypes(field.FieldType, assemblies, processed);

                var method = member as MethodBase;
                if (method == null)
                    continue;

                foreach (var parameter in method.GetParameters())
                    CollectAssembliesFromTypes(parameter.ParameterType, assemblies, processed);

                if (method.IsGenericMethod && !method.IsGenericMethodDefinition)
                    foreach (var argument in method.GetGenericArguments())
                        CollectAssembliesFromTypes(argument, assemblies, processed);

                var methodInfo = method as MethodInfo;
                if (methodInfo != null)
                    CollectAssembliesFromTypes(methodInfo.ReturnType, assemblies, processed);
            }
        }

        /// <summary>
        /// Преобразовать список сборок в список ссылок для парсера
        /// </summary>
        /// <param name="references"></param>
        /// <returns></returns>
        private static IEnumerable<MetadataReference> CollectReferences(List<Assembly> references)
        {
            var processed = new HashSet<string>();
            foreach (var assembly in references)
            {
                if (assembly.IsDynamic)
                    continue;

                if (!processed.Contains(assembly.Location))
                    processed.Add(assembly.Location);
            }

            return processed.Select(x => MetadataReference.CreateFromFile(x));
        }
    }
}