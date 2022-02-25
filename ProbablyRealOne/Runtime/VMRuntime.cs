using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace SugarGuard.Runtime
{
    public static class VMRuntime
    {
        public static BinaryReader Reader;
        public static Dictionary<short, OpCode> OpCodeList;
        public static Assembly Asm;
        public static void Initialize(string resource)
        {
            var asm = Assembly.GetCallingAssembly();
            var stream = asm.GetManifestResourceStream(resource);
            var memStream = new MemoryStream();
            stream.CopyTo(memStream);
            Asm = asm;
            Reader = new BinaryReader(memStream);
            OpCodeList = new Dictionary<short, OpCode>();
            foreach (var f in typeof(OpCodes).GetFields())
            {
                if (f.FieldType == typeof(OpCode))
                {
                    var opCode = (OpCode)f.GetValue(null);
                    OpCodeList.Add(opCode.Value, opCode);
                }
            }
        }

        public static object Execute(int offset, object[] parameters)
        {
            Reader.BaseStream.Position = offset;

            var module = Asm.ManifestModule;
            var token = Reader.ReadInt32();
            var method = module.ResolveMethod(token);

            var instrs = Reader.ReadInt32();
            var targets = Reader.ReadInt32();
            var methodParams = method.GetParameters();
            var methodLocals = method.GetMethodBody().LocalVariables;
            var labels = new Dictionary<int, Label>();
            var dynLocals = new List<LocalBuilder>();
            var dynParams = new Type[methodParams.Length];
            var loopIndex = 0;

            if (!method.IsStatic)
            {
                loopIndex = 1;
                dynParams = new Type[methodParams.Length + 1];
                dynParams[0] = method.DeclaringType;
            }

            for (; loopIndex < methodParams.Length; loopIndex++)
                dynParams[loopIndex] = methodParams[loopIndex].ParameterType;

            var returnType = (method as MethodInfo).ReturnType;
            var dynMethod = new DynamicMethod("", returnType, dynParams, true);
            var il = dynMethod.GetILGenerator();

            for (int k = 0; k < methodLocals.Count; k++)
                dynLocals.Add(il.DeclareLocal(methodLocals[k].LocalType, methodLocals[k].IsPinned));

            for (int l = 0; l < targets; l++)
                labels.Add(Reader.ReadInt32(), il.DefineLabel());

            for (int j = 0; j < instrs; j++)
            {
                if (labels.ContainsKey(j))
                    il.MarkLabel(labels[j]);

                var exceptionLoop = Reader.ReadInt32();
                for (int e = 0; e < exceptionLoop; e++)
                {
                    var exceptionType = Reader.ReadInt32();
                    switch (exceptionType)
                    {
                        case 0:
                            il.BeginExceptionBlock();
                            break;
                        case 1:
                            il.BeginExceptFilterBlock();
                            break;
                        case 2:
                            var catchTypeToken = Reader.ReadInt32();
                            if (catchTypeToken == -1)
                            {
                                il.BeginCatchBlock(typeof(object));
                                continue;
                            }
                            var catchType = module.ResolveType(catchTypeToken);
                            il.BeginCatchBlock(catchType);
                            break;
                        case 3:
                            il.BeginFinallyBlock();
                            break;
                        case 4:
                            il.BeginFaultBlock();
                            break;
                        case 5:
                            il.EndExceptionBlock();
                            break;
                    }
                }

                var s = Reader.ReadInt16();
                var opCode = OpCodeList[s];
                var operandType = Reader.ReadInt32();
                switch (operandType)
                {
                    case 0:
                        il.Emit(opCode);
                        break;
                    case 1:
                        il.Emit(opCode, Reader.ReadString());
                        break;
                    case 2:
                        il.Emit(opCode, Reader.ReadDouble());
                        break;
                    case 3:
                        il.Emit(opCode, Reader.ReadInt64());
                        break;
                    case 4:
                        il.Emit(opCode, Reader.ReadInt32());
                        break;
                    case 5:
                        il.Emit(opCode, Reader.ReadSingle());
                        break;
                    case 6:
                        il.Emit(opCode, Reader.ReadByte());
                        break;
                    case 7:
                        var typeToken = Reader.ReadInt32();
                        var type = module.ResolveType(typeToken);
                        il.Emit(opCode, type);
                        break;
                    case 8:
                        var fieldToken = Reader.ReadInt32();
                        var field = module.ResolveField(fieldToken);
                        il.Emit(opCode, field);
                        break;
                    case 9:
                        var methodToken = Reader.ReadInt32();
                        var cmethod = module.ResolveMethod(methodToken);
                        if (cmethod is MethodInfo mInfo)
                            il.Emit(opCode, mInfo);
                        else
                            il.Emit(opCode, cmethod as ConstructorInfo);
                        break;
                    case 10:
                        var tokToken = Reader.ReadInt32();
                        var tokType = Reader.ReadInt32();
                        switch (tokType)
                        {
                            case 0:
                                var tokRField = module.ResolveField(tokToken);
                                il.Emit(opCode, tokRField);
                                break;
                            case 1:
                                var tokRType = module.ResolveType(tokToken);
                                il.Emit(opCode, tokRType);
                                break;
                            case 2:
                                var tokRMethod = module.ResolveMethod(tokToken);
                                if (tokRMethod is MethodInfo info)
                                    il.Emit(opCode, info);
                                else
                                    il.Emit(opCode, tokRMethod as ConstructorInfo);
                                break;
                        }
                        break;
                    case 11:
                        var target = Reader.ReadInt32();
                        var label = labels[target];
                        il.Emit(opCode, label);
                        break;
                    case 12:
                        var index = Reader.ReadInt32();
                        var indexType = Reader.ReadInt32();
                        switch (indexType)
                        {
                            case 0:
                                il.Emit(opCode, dynLocals[index]);
                                break;
                            case 1:
                                il.Emit(opCode, index);
                                break;
                        }
                        break;
                    case 13:
                        var switchCount = Reader.ReadInt32();
                        var switchLabels = new Label[switchCount];
                        for (int u = 0; u < switchCount; u++)
                            switchLabels[u] = labels[Reader.ReadInt32()];
                        il.Emit(opCode, switchLabels);
                        break;
                }
            }
            return dynMethod.Invoke(null, parameters);
        }
    }
}
