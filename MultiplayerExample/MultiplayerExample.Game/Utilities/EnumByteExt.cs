using System;
using System.Reflection.Emit;

namespace MultiplayerExample.Utilities
{
    static class EnumByteExt<T> where T : Enum
    {
        private static Func<byte, T> CreateByteToEnumConverter()
        {
            var method = new DynamicMethod(
                name: "ConvertByteToEnum",
                returnType: typeof(T),
                parameterTypes: new[] { typeof(byte) },
                m: typeof(EnumByteExt<T>).Module,
                skipVisibility: true);

            ILGenerator ilGen = method.GetILGenerator();
            // Declare a local variable of the enum type
            LocalBuilder enumVar = ilGen.DeclareLocal(typeof(T));
            // Push the byte arg on the stack
            ilGen.Emit(OpCodes.Ldarg_0);
            // Store the byte at the enum variable
            ilGen.Emit(OpCodes.Stloc, enumVar);
            // Push the enum variable to return
            ilGen.Emit(OpCodes.Ldloc, enumVar);
            ilGen.Emit(OpCodes.Ret);

            var byteToEnumFunc = (Func<byte, T>)method.CreateDelegate(typeof(Func<byte, T>));
            return byteToEnumFunc;
        }

        public static Func<byte, T> ToEnumConverter { get; } = CreateByteToEnumConverter();
    }
}
