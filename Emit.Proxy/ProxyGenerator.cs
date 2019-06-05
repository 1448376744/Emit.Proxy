using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Emit.Proxy
{
    public class ProxyGenerator
    {
        private readonly List<DynamicAssembly> _dynamicAssemblys = new List<DynamicAssembly>();
        public List<DynamicAssembly> DynamicAssemblys => _dynamicAssemblys;
        public T CreateInstanse<T>(IInterceptor interceptor) where T : class
        {
            var proxyType = _dynamicAssemblys
                .Find(f => f.TargetAssembly == typeof(T).Assembly)?.DynamicTypes
                .Find(f => f.TargetType == typeof(T))?.ProxyType
                ?? CreateProxyType(typeof(T));
            var target = Activator.CreateInstance(typeof(T));
            var proxy = Activator.CreateInstance(proxyType, new object[] { target, interceptor }) as T;
         
            return proxy;
        }
        public Type CreateProxyType(Type targetType)
        {
            var moduleBuilder = _dynamicAssemblys.Find(f => f.TargetAssembly == targetType.Assembly)?.ModuleBuilder
                ?? CreateAssembly(targetType);
            //builder type
            var typeBuilder = moduleBuilder.DefineType(targetType.Name + "Proxy", TypeAttributes.Public | TypeAttributes.Class, targetType);
            //builder field
            var interceptorFieldBuilder = typeBuilder.DefineField("_interceptor", typeof(IInterceptor), FieldAttributes.Public);
            var targetFieldBuilder = typeBuilder.DefineField("_target", targetType, FieldAttributes.Public);
            //builder ctor()
            var ctor0Builder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
            var ctor0IL = ctor0Builder.GetILGenerator();
            //edit body
            ctor0IL.Emit(OpCodes.Nop);
            ctor0IL.Emit(OpCodes.Ret);
            //builder ctor(T target,IInterceptor interceptor)
            var ctor2Builder = typeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig, CallingConventions.Standard, new Type[] { targetType, typeof(IInterceptor) });
            ctor2Builder.DefineParameter(1, ParameterAttributes.None, "target");
            ctor2Builder.DefineParameter(2, ParameterAttributes.None, "interceptor");
            var ctor2IL = ctor2Builder.GetILGenerator();
            //edit body
            ctor2IL.Emit(OpCodes.Ldarg_0);
            ctor2IL.Emit(OpCodes.Ldarg_1);
            ctor2IL.Emit(OpCodes.Stfld, targetFieldBuilder);
            ctor2IL.Emit(OpCodes.Ldarg_0);
            ctor2IL.Emit(OpCodes.Ldarg_2);
            ctor2IL.Emit(OpCodes.Stfld, interceptorFieldBuilder);
            ctor2IL.Emit(OpCodes.Ret);
            //override method
            foreach (var method in targetType.GetMethods().Where(a => a.IsVirtual && a.DeclaringType != typeof(object)))
            {
                //builder method
                var methodBuilder = typeBuilder.DefineMethod(method.Name,
                        MethodAttributes.Virtual | MethodAttributes.Public | MethodAttributes.HideBySig,
                        method.ReturnType, method.GetParameters().Select(s => s.ParameterType).ToArray());
                //define parameter
                for (int i = 0; i < method.GetParameters().Length; i++)
                {
                    methodBuilder.DefineParameter(i + 1, method.GetParameters()[i].Attributes, method.GetParameters()[i].Name);
                }
                //edit body
                ILGenerator methodIL = methodBuilder.GetILGenerator();
                //init local
                //DefaultInvocation loc0 = null;
                methodIL.DeclareLocal(typeof(DefaultInvocation));
                if (method.ReturnType != typeof(void))
                {
                    methodIL.DeclareLocal(method.ReturnType);
                }

                methodIL.Emit(OpCodes.Nop);
                //loc0 = new DefaultInvocation();
                methodIL.Emit(OpCodes.Newobj, typeof(DefaultInvocation).GetConstructor(Type.EmptyTypes));
                methodIL.Emit(OpCodes.Stloc_0);
                //loc0.Target = _target;
                methodIL.Emit(OpCodes.Ldloc_0);
                methodIL.Emit(OpCodes.Ldarg_0);
                methodIL.Emit(OpCodes.Ldfld, targetFieldBuilder);
                methodIL.Emit(OpCodes.Callvirt, typeof(DefaultInvocation).GetProperty(nameof(DefaultInvocation.Target)).GetSetMethod());
                methodIL.Emit(OpCodes.Nop);
                //loc0.TargetType = typeof(T);
                methodIL.Emit(OpCodes.Ldloc_0);
                methodIL.Emit(OpCodes.Ldtoken, targetType);
                methodIL.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), new Type[] { typeof(RuntimeTypeHandle) }));
                methodIL.Emit(OpCodes.Callvirt, typeof(DefaultInvocation).GetProperty(nameof(DefaultInvocation.TargetType)).GetSetMethod());
                methodIL.Emit(OpCodes.Nop);
                //loc0.TargetMethod = typeof(T).GetMethod(name,new Type[]);
                methodIL.Emit(OpCodes.Ldloc_0);
                methodIL.Emit(OpCodes.Ldtoken, targetType);
                methodIL.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), new Type[] { typeof(RuntimeTypeHandle) }));
                methodIL.Emit(OpCodes.Ldstr, method.Name);
                if (method.GetParameters().Select(s => s.ParameterType).Count() > 0)
                {
                    methodIL.Emit(OpCodes.Ldc_I4_S, method.GetParameters().Select(s => s.ParameterType).Count());
                    methodIL.Emit(OpCodes.Newarr, typeof(Type));
                    methodIL.Emit(OpCodes.Dup);
                    for (int i = 0; i < method.GetParameters().Select(s => s.ParameterType).Count(); i++)
                    {
                        methodIL.Emit(OpCodes.Ldc_I4, i);
                        methodIL.Emit(OpCodes.Ldtoken, method.GetParameters()[i].ParameterType);
                        methodIL.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), new Type[] { typeof(RuntimeTypeHandle) }));
                        methodIL.Emit(OpCodes.Stelem_Ref);
                        if (i < method.GetParameters().Select(s => s.ParameterType).Count() - 1)
                        {
                            methodIL.Emit(OpCodes.Dup);
                        }
                    }
                }
                else
                {
                    methodIL.Emit(OpCodes.Ldsfld, typeof(Type).GetField(nameof(Type.EmptyTypes)));
                }
                methodIL.Emit(OpCodes.Callvirt, typeof(Type).GetMethod(nameof(Type.GetMethod), new Type[] { typeof(string), typeof(Type[]) }));
                methodIL.Emit(OpCodes.Callvirt, typeof(DefaultInvocation).GetProperty(nameof(DefaultInvocation.TargetMethod)).GetSetMethod());
                methodIL.Emit(OpCodes.Nop);
                //loc0.Arguments = new object[2];
                methodIL.Emit(OpCodes.Ldloc_0);
                methodIL.Emit(OpCodes.Ldc_I4_S, method.GetParameters().Select(s => s.ParameterType).Count());
                methodIL.Emit(OpCodes.Newarr, typeof(object));
                methodIL.Emit(OpCodes.Callvirt, typeof(DefaultInvocation).GetProperty(nameof(DefaultInvocation.Arguments)).GetSetMethod());
                methodIL.Emit(OpCodes.Nop);
                //loc0.Arguments[i]=argi;
                if (method.GetParameters().Select(s => s.ParameterType).Count() > 0)
                {
                    for (int i = 0; i < method.GetParameters().Select(s => s.ParameterType).Count(); i++)
                    {
                        methodIL.Emit(OpCodes.Ldloc_0);
                        methodIL.Emit(OpCodes.Callvirt, typeof(DefaultInvocation).GetProperty(nameof(DefaultInvocation.Arguments)).GetGetMethod());
                        methodIL.Emit(OpCodes.Ldc_I4, i);
                        methodIL.Emit(OpCodes.Ldarg, i + 1);
                        if (method.GetParameters()[i].ParameterType.IsValueType)
                        {
                            methodIL.Emit(OpCodes.Box, method.GetParameters()[i].ParameterType);
                        }
                        methodIL.Emit(OpCodes.Stelem_Ref);
                    }
                }
                //_interceptors.Intercept(invocation);
                methodIL.Emit(OpCodes.Ldarg_0);
                methodIL.Emit(OpCodes.Ldfld, interceptorFieldBuilder);
                methodIL.Emit(OpCodes.Ldloc_0);
                methodIL.Emit(OpCodes.Callvirt, typeof(IInterceptor).GetMethod(nameof(IInterceptor.Intercept)));
                if (method.ReturnType != typeof(void))
                {
                    //return loc0.ReturnValue;
                    methodIL.Emit(OpCodes.Nop);
                    methodIL.Emit(OpCodes.Ldloc_0);
                    methodIL.Emit(OpCodes.Callvirt, typeof(DefaultInvocation).GetProperty(nameof(DefaultInvocation.ReturnValue)).GetGetMethod());
                    if (method.ReturnType.IsValueType)
                    {
                        methodIL.Emit(OpCodes.Unbox_Any, method.ReturnType);
                    }
                    methodIL.Emit(OpCodes.Stloc_1);
                    methodIL.Emit(OpCodes.Br_S);
                    methodIL.Emit(OpCodes.Nop);
                    methodIL.Emit(OpCodes.Ldloc_1);
                    methodIL.Emit(OpCodes.Ret);
                }
                else
                {
                    //return
                    methodIL.Emit(OpCodes.Ret);
                }
            }
            //create type
            var proxyType = typeBuilder.CreateTypeInfo();
            //cache
            _dynamicAssemblys.Find(f => f.TargetAssembly == targetType.Assembly).DynamicTypes.Add(new DynamicType()
            {
                ProxyType= proxyType,
                TargetType=targetType,
            });
            return proxyType;
        }
        public ModuleBuilder CreateAssembly(Type targetType)
        {
            var assemblyName = targetType.Assembly.GetName();
            assemblyName.Name += "Proxy";
            var proxyAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = proxyAssembly.DefineDynamicModule(targetType.Module.Name.Insert(targetType.Module.Name.IndexOf("."), "Proxy"));
            //cache
            _dynamicAssemblys.Add(new DynamicAssembly()
            {
                ProxyAssembly = proxyAssembly,
                TargetAssembly = targetType.Assembly,
                ModuleBuilder = moduleBuilder
            });
            return moduleBuilder;
        }
    }
    public class DynamicAssembly
    {
        public ModuleBuilder ModuleBuilder { get; set; }
        public AssemblyBuilder ProxyAssembly { get; set; }
        public Assembly TargetAssembly { get; set; }
        public List<DynamicType> DynamicTypes = new List<DynamicType>();
    }
    public class DynamicType
    {
        public Type TargetType { get; set; }
        public Type ProxyType { get; set; }
    }
}
