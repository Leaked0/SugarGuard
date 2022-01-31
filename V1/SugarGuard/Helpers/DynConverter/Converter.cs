using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SugarGuard.Helpers.DynConverter
{
    public class Converter
    {
        public MethodDef Method { get; }
        public BinaryWriter Writer { get; }
        public Converter(MethodDef method, BinaryWriter writer)
        {
            Method = method;
            Writer = writer;

            method.Body.SimplifyMacros(method.Parameters);
            method.Body.SimplifyBranches();
        }

        public void ConvertToBytes()
        {
            var mapper = new ExceptionMapper(Method);
            var instrs = Method.Body.Instructions;
            var count = instrs.Count;
            var targets = new List<int>();

            Writer.Write(count);

            for (int i = 0; i < count; i++)
            {

                var type = instrs[i].OpCode.OperandType;
                switch (type)
                {
                    case OperandType.InlineBrTarget:
                    case OperandType.ShortInlineBrTarget:
                        targets.Add(instrs.IndexOf(instrs[i].Operand as Instruction));
                        break;
                    case OperandType.InlineSwitch:
                        var operand = instrs[i].Operand as Instruction[];
                        foreach (var ins in operand)
                            targets.Add(instrs.IndexOf(ins));
                        break;
                }
            }

            Writer.Write(targets.Count);

            foreach (int target in targets)
                Writer.Write(target);

            for (int i =0; i < count; i++)
            {
                var instr = instrs[i];
                var value = instr.OpCode.Value;
                var type = instr.OpCode.OperandType;
                var operand = instr.Operand;

                mapper.MapAndWrite(Writer, instr);
                Writer.Write(value);

                switch (type)
                {
                    case OperandType.InlineNone:
                        Writer.EmitNone();
                        break;
                    case OperandType.InlineString:
                        Writer.EmitString(instr);
                        break;
                    case OperandType.InlineI:
                        Writer.EmitI(instr);
                        break;
                    case OperandType.InlineI8:
                        Writer.EmitI8(instr);
                        break;
                    case OperandType.InlineR:
                        Writer.EmitR(instr);
                        break;
                    case OperandType.ShortInlineR:
                        Writer.EmitShortR(instr);
                        break;
                    case OperandType.ShortInlineI:
                        Writer.EmitShortI(instr);
                        break;
                    case OperandType.InlineType:
                        Writer.EmitType(instr);
                        break;
                    case OperandType.InlineField:
                        Writer.EmitField(instr);
                        break;
                    case OperandType.InlineMethod:
                        Writer.EmitMethod(instr);
                        break;
                    case OperandType.InlineTok:
                        Writer.EmitTok(instr);
                        break;
                    case OperandType.InlineBrTarget:
                    case OperandType.ShortInlineBrTarget:
                        Writer.EmitBr(instrs.IndexOf(operand as Instruction));
                        break;
                    case OperandType.ShortInlineVar:
                    case OperandType.InlineVar:
                        Writer.EmitVar(instr);
                        break;
                    case OperandType.InlineSwitch:
                        Writer.EmitSwitch(instrs.ToList(), instr);
                        break;

                }
            }
        }
        private class ExceptionInfo
        {
            public int Type { get; }
            public int Action { get; }
            public ExceptionInfo(int type, int action)
            {
                Type = type;
                Action = action;
            }
        }

    }
}
