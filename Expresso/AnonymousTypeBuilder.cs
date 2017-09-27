namespace Expresso
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// Статический класс для построения типов используемых в выражениях
    /// </summary>
    internal static class AnonymousTypeBuilder
    {
        private static readonly AssemblyName AssemblyName = new AssemblyName { Name = "AnonymousTypesAssembly" };

        private static readonly ModuleBuilder ModuleBuilder;

        private static readonly Dictionary<string, Type> BuiltTypes = new Dictionary<string, Type>();

        private static int _instanceId;

        static AnonymousTypeBuilder()
        {
            ModuleBuilder = Thread.GetDomain()
                .DefineDynamicAssembly(AssemblyName, AssemblyBuilderAccess.Run)
                .DefineDynamicModule(AssemblyName.Name);
        }

        /// <summary>
        /// Создание типа, содержащего свойства, переданые в параметре <see cref="properties"/>.
        /// Также для типа формируется конструктор по-умолчанию и конструктор с аргументами для инициализации свойств
        /// </summary>
        /// <param name="properties">Набор свойств типа</param>        
        public static Type Build(IDictionary<string, Type> properties)
        {
            var typeKey = GetTypeKey(properties);
            Type anonymousType;

            lock (BuiltTypes)
            {
                if (BuiltTypes.TryGetValue(typeKey, out anonymousType))
                {
                    return anonymousType;
                }

                var className = "Expresso.Generated.AnonymousType<" + _instanceId.ToString("X") + ">";
                Interlocked.Increment(ref _instanceId);

                var builder = ModuleBuilder.DefineType(className, TypeAttributes.BeforeFieldInit | TypeAttributes.Sealed);
                var fieldBuilders = new List<FieldBuilder>();

                foreach (var pair in properties)
                {
                    var fieldName = "_" + pair.Key.ToLowerInvariant();

                    var field = builder.DefineField(fieldName, pair.Value, FieldAttributes.InitOnly | FieldAttributes.Private);
                    fieldBuilders.Add(field);

                    GenerateProperty(builder, pair.Key, field);
                }

                var propertyNames = properties.Keys.ToArray();

                GenerateClassAttributes(builder, propertyNames);
                GenerateConstructor(builder, propertyNames, fieldBuilders);
                GenerateEqualsMethod(builder, fieldBuilders.ToArray());
                GenerateGetHashCodeMethod(builder, fieldBuilders.ToArray());
                GenerateToStringMethod(builder, propertyNames, fieldBuilders.ToArray());

                BuiltTypes[typeKey] = anonymousType = builder.CreateType();
            }

            return anonymousType;
        }

        private static string GetTypeKey(IDictionary<string, Type> fields)
        {
            return fields.OrderBy(x => x.Key, StringComparer.Ordinal).Aggregate(string.Empty, (current, field) => current + (field.Key + ";" + field.Value.Name + ";"));
        }

        private static void AddDebuggerHiddenAttribute(ConstructorBuilder constructor)
        {
            var type = typeof(DebuggerHiddenAttribute);
            var customBuilder = new CustomAttributeBuilder(type.GetConstructor(new Type[0]), new object[0]);
            constructor.SetCustomAttribute(customBuilder);
        }

        private static void AddDebuggerHiddenAttribute(MethodBuilder method)
        {
            var type = typeof(DebuggerHiddenAttribute);
            var customBuilder = new CustomAttributeBuilder(type.GetConstructor(new Type[0]), new object[0]);
            method.SetCustomAttribute(customBuilder);
        }

        private static void GenerateClassAttributes(TypeBuilder dynamicType, string[] properties)
        {
            var type = typeof(CompilerGeneratedAttribute);
            var customBuilder = new CustomAttributeBuilder(type.GetConstructor(new Type[0]), new object[0]);
            dynamicType.SetCustomAttribute(customBuilder);
            var type2 = typeof(DebuggerDisplayAttribute);
            var builder2 = new StringBuilder(@"\{ ");
            var flag = true;
            foreach (var propertyName in properties)
            {
                builder2.AppendFormat("{0}{1} = ", flag ? string.Empty : ", ", propertyName);
                builder2.Append("{");
                builder2.Append(propertyName);
                builder2.Append("}");
                flag = false;
            }

            builder2.Append(" }");
            var property = type2.GetProperty("Type");
            var builder3 = new CustomAttributeBuilder(type2.GetConstructor(new[] { typeof(string) }), new object[] { builder2.ToString() }, new[] { property }, new object[] { "<Anonymous Type>" });
            dynamicType.SetCustomAttribute(builder3);
        }

        private static void GenerateConstructor(TypeBuilder dynamicType, string[] properties, List<FieldBuilder> fields)
        {
            const MethodAttributes CtorAttributes = MethodAttributes.RTSpecialName | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Public;
            var defaultCtor = dynamicType.DefineDefaultConstructor(CtorAttributes);

            var ctor = dynamicType.DefineConstructor(CtorAttributes, CallingConventions.Standard, fields.Select(x => x.FieldType).ToArray());
            var ctorIl = ctor.GetILGenerator();
            ctorIl.Emit(OpCodes.Ldarg_0);
            ctorIl.Emit(OpCodes.Call, defaultCtor);

            for (var i = 0; i < properties.Length; i++)
            {
                var strParamName = properties[i];
                var field = fields[i];
                var builder3 = ctor.DefineParameter(i + 1, ParameterAttributes.None, strParamName);
                ctorIl.Emit(OpCodes.Ldarg_0);
                ctorIl.Emit(OpCodes.Ldarg, builder3.Position);
                ctorIl.Emit(OpCodes.Stfld, field);
            }

            ctorIl.Emit(OpCodes.Ret);

            AddDebuggerHiddenAttribute(ctor);
        }

        private static void GenerateEqualsMethod(TypeBuilder dynamicType, FieldBuilder[] fields)
        {
            var methodInfoBody = dynamicType.DefineMethod("Equals", MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Public, CallingConventions.Standard, typeof(bool), new[] { typeof(object) });
            methodInfoBody.DefineParameter(0, ParameterAttributes.None, "value");
            var generator = methodInfoBody.GetILGenerator();
            var local = generator.DeclareLocal(dynamicType);
            var label = generator.DefineLabel();
            var label2 = generator.DefineLabel();
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Isinst, dynamicType);
            generator.Emit(OpCodes.Stloc, local);
            generator.Emit(OpCodes.Ldloc, local);
            var genericComparerType = typeof(EqualityComparer<>);

            foreach (var fieldBuilder in fields)
            {
                var comparerType = genericComparerType.MakeGenericType(fieldBuilder.FieldType);
                var getMethod = genericComparerType.GetProperty("Default", BindingFlags.Public | BindingFlags.Static).GetGetMethod();
                ////var method = TypeBuilder.GetMethod(comparerType, getMethod);

                generator.Emit(OpCodes.Brfalse_S, label);
                generator.EmitCall(OpCodes.Call, getMethod, null);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, fieldBuilder);
                generator.Emit(OpCodes.Ldloc, local);
                generator.Emit(OpCodes.Ldfld, fieldBuilder);

                var type3 = genericComparerType.GetGenericArguments()[0];
                var info3 = genericComparerType.GetMethod("Equals", BindingFlags.Public | BindingFlags.Instance, null, new[] { type3, type3 }, null);
                ////var methodInfo = TypeBuilder.GetMethod(comparerType, info3);
                generator.EmitCall(OpCodes.Callvirt, info3, null);
            }

            generator.Emit(OpCodes.Br_S, label2);
            generator.MarkLabel(label);
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.MarkLabel(label2);
            generator.Emit(OpCodes.Ret);
            dynamicType.DefineMethodOverride(methodInfoBody, typeof(object).GetMethod("Equals", BindingFlags.Public | BindingFlags.Instance));

            AddDebuggerHiddenAttribute(methodInfoBody);
        }

        private static void GenerateGetHashCodeMethod(TypeBuilder dynamicType, FieldBuilder[] fields)
        {
            var methodInfoBody = dynamicType.DefineMethod("GetHashCode", MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Public, CallingConventions.Standard, typeof(int), new Type[0]);
            var generator = methodInfoBody.GetILGenerator();
            var type = typeof(EqualityComparer<>);
            var local = generator.DeclareLocal(typeof(int));
            generator.Emit(OpCodes.Ldc_I4, -747105811);
            generator.Emit(OpCodes.Stloc, local);
            foreach (var builder3 in fields)
            {
                generator.Emit(OpCodes.Ldc_I4, -1521134295);
                generator.Emit(OpCodes.Ldloc, local);
                generator.Emit(OpCodes.Mul);
                var type2 = type.MakeGenericType(builder3.FieldType);
                var getMethod = type.GetProperty("Default", BindingFlags.Public | BindingFlags.Static).GetGetMethod();
                ////var method = TypeBuilder.GetMethod(type2, getMethod);
                generator.EmitCall(OpCodes.Call, getMethod, null);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, builder3);
                var type3 = type.GetGenericArguments()[0];
                var info3 = type.GetMethod("GetHashCode", BindingFlags.Public | BindingFlags.Instance, null, new[] { type3 }, null);
                ////var methodInfo = TypeBuilder.GetMethod(type2, info3);
                generator.EmitCall(OpCodes.Callvirt, info3, null);
                generator.Emit(OpCodes.Add);
                generator.Emit(OpCodes.Stloc, local);
            }

            generator.Emit(OpCodes.Ldloc, local);
            generator.Emit(OpCodes.Ret);
            dynamicType.DefineMethodOverride(methodInfoBody, typeof(object).GetMethod("GetHashCode", BindingFlags.Public | BindingFlags.Instance));

            AddDebuggerHiddenAttribute(methodInfoBody);
        }

        private static void GenerateProperty(TypeBuilder dynamicType, string propertyName, FieldBuilder field)
        {
            const MethodAttributes AccessorAttributes = MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Public;

            var property = dynamicType.DefineProperty(propertyName, PropertyAttributes.None, field.FieldType, null);

            var getter = dynamicType.DefineMethod($"get_{property.Name}", AccessorAttributes);
            getter.SetReturnType(field.FieldType);
            var getterIl = getter.GetILGenerator();
            getterIl.Emit(OpCodes.Ldarg_0);
            getterIl.Emit(OpCodes.Ldfld, field);
            getterIl.Emit(OpCodes.Ret);
            property.SetGetMethod(getter);

            var setter = dynamicType.DefineMethod($"set_{property.Name}", AccessorAttributes);
            var setterIl = setter.GetILGenerator();
            setterIl.Emit(OpCodes.Ldarg_0);
            setterIl.Emit(OpCodes.Ldarg_1);
            setterIl.Emit(OpCodes.Stfld, field);
            property.SetSetMethod(setter);
        }

        private static void GenerateToStringMethod(TypeBuilder dynamicType, string[] propertyNames, FieldBuilder[] fields)
        {
            var methodInfoBody = dynamicType.DefineMethod("ToString", MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Public, CallingConventions.Standard, typeof(string), new Type[0]);
            var generator = methodInfoBody.GetILGenerator();
            var local = generator.DeclareLocal(typeof(StringBuilder));
            var methodInfo = typeof(StringBuilder).GetMethod("Append", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(object) }, null);
            var info2 = typeof(StringBuilder).GetMethod("Append", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
            var info3 = typeof(object).GetMethod("ToString", BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null);
            generator.Emit(OpCodes.Newobj, typeof(StringBuilder).GetConstructor(new Type[0]));
            generator.Emit(OpCodes.Stloc, local);
            generator.Emit(OpCodes.Ldloc, local);
            generator.Emit(OpCodes.Ldstr, "{ ");
            generator.EmitCall(OpCodes.Callvirt, info2, null);
            generator.Emit(OpCodes.Pop);

            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                generator.Emit(OpCodes.Ldloc, local);
                generator.Emit(OpCodes.Ldstr, (i == 0 ? string.Empty : ", ") + propertyNames[i] + " = ");
                generator.EmitCall(OpCodes.Callvirt, info2, null);
                generator.Emit(OpCodes.Pop);
                generator.Emit(OpCodes.Ldloc, local);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, field);
                generator.Emit(OpCodes.Box, field.FieldType);
                generator.EmitCall(OpCodes.Callvirt, methodInfo, null);
                generator.Emit(OpCodes.Pop);
            }

            generator.Emit(OpCodes.Ldloc, local);
            generator.Emit(OpCodes.Ldstr, " }");
            generator.EmitCall(OpCodes.Callvirt, info2, null);
            generator.Emit(OpCodes.Pop);
            generator.Emit(OpCodes.Ldloc, local);
            generator.EmitCall(OpCodes.Callvirt, info3, null);
            generator.Emit(OpCodes.Ret);
            dynamicType.DefineMethodOverride(methodInfoBody, typeof(object).GetMethod("ToString", BindingFlags.Public | BindingFlags.Instance));

            AddDebuggerHiddenAttribute(methodInfoBody);
        }
    }
}