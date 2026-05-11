using Mpt.Framework.Operation.Models;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace Mpt.Framework.Operation.Utility;

internal static class OperationSagaTypeBuilder
{
    private static readonly ModuleBuilder _moduleBuilder = InitBuilder();

    private static readonly ConcurrentDictionary<Type, Type> _createdTypes = [];
    private static long _typeCounter = 0;

    static ModuleBuilder InitBuilder()
    {
        AssemblyName assemblyName = new("DynamicOperationAssembly");
        AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        return assemblyBuilder.DefineDynamicModule("DynamicOperationModule");
    }

    public static Type MakeSagaType(Type operation, string type)
    {
        return _createdTypes.GetOrAdd(operation, t =>
        {
            var uniqueId = Interlocked.Increment(ref _typeCounter);
            TypeBuilder typeBuilder = _moduleBuilder.DefineType(
                $"{t.Name}Saga_{uniqueId}",
                TypeAttributes.Public | TypeAttributes.Class,
                typeof(OperationSaga));

            // Find base constructor with single string parameter
            ConstructorInfo? baseCtor = typeof(OperationSaga).GetConstructor([typeof(string)])
                ?? throw new InvalidOperationException("Base type must have a constructor with a single string parameter.");

            // Define parameterless derived constructor
            ConstructorBuilder ctorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                Type.EmptyTypes // no parameters
            );

            ILGenerator il = ctorBuilder.GetILGenerator();

            // Emit IL to call base constructor with constant string
            il.Emit(OpCodes.Ldarg_0);               // load 'this'
            il.Emit(OpCodes.Ldstr, type);           // load constant string
            il.Emit(OpCodes.Call, baseCtor);        // call base(string)
            il.Emit(OpCodes.Ret);

            return typeBuilder.CreateType();
        });
    }
}
