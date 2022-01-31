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
    public static class Emitter
    {
        public static void EmitNone(this BinaryWriter writer)
        {
            writer.Write(0);
        }
        public static void EmitString(this BinaryWriter writer, Instruction instr)
        {
            writer.Write(1);
            writer.Write(instr.Operand.ToString());
        }

        public static void EmitR(this BinaryWriter writer, Instruction instr)
        {
            writer.Write(2);
            writer.Write((double)instr.Operand);
        }

        public static void EmitI8(this BinaryWriter writer, Instruction instr)
        {
            writer.Write(3);
            writer.Write((long)instr.Operand);
        }

        public static void EmitI(this BinaryWriter writer, Instruction instr)
        {
            writer.Write(4);
            writer.Write(instr.GetLdcI4Value());
        }

        public static void EmitShortR(this BinaryWriter writer, Instruction instr)
        {
            writer.Write(5);
            writer.Write((float)instr.Operand);
        }

        public static void EmitShortI(this BinaryWriter writer, Instruction instr)
        {
            writer.Write(6);
            writer.Write((byte)instr.GetLdcI4Value());
        }

        public static void EmitType(this BinaryWriter writer, Instruction instr)
        {
            writer.Write(7);
            var typeDeforRef = instr.Operand as ITypeDefOrRef;
            writer.Write(typeDeforRef.MDToken.ToInt32());
        }

        public static void EmitField(this BinaryWriter writer, Instruction instr)
        {
            writer.Write(8);
            var field = instr.Operand as IField;
            writer.Write(field.MDToken.ToInt32());
        }

        public static void EmitMethod(this BinaryWriter writer, Instruction instr)
        {
            writer.Write(9);
            if (instr.Operand is MethodSpec spec)
            {
                writer.Write(spec.MDToken.ToInt32());
            }
            else
            {
                var method = instr.Operand as IMethodDefOrRef;
                writer.Write(method.MDToken.ToInt32());
            }
        }

        public static void EmitTok(this BinaryWriter writer, Instruction instr)
        {
            writer.Write(10);
            var operand = instr.Operand;
            if (operand is IField field)
            {
                writer.Write(field.MDToken.ToInt32());
                writer.Write(0);
            }
            else if (operand is ITypeDefOrRef type)
            {
                writer.Write(type.MDToken.ToInt32());
                writer.Write(1);
            }
            else
            {
                writer.Write((operand as IMethodDefOrRef).MDToken.ToInt32());
                writer.Write(2);
            }
        }

        public static void EmitBr(this BinaryWriter writer, int index)
        {
            writer.Write(11);
            writer.Write(index);
        }

        public static void EmitVar(this BinaryWriter writer, Instruction instr)
        {
            writer.Write(12);
            if (instr.Operand is Local local)
            {
                writer.Write(local.Index);
                writer.Write(0);
            }
            else if (instr.Operand is Parameter param)
            {
                writer.Write(param.Index);
                writer.Write(1);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static void EmitSwitch(this BinaryWriter writer, List<Instruction> instrs, Instruction instr)
        {
            writer.Write(13);
            var instructions = instr.Operand as Instruction[];
            writer.Write(instructions.Length);
            foreach (var i in instructions)
            {
                int index = instrs.IndexOf(i);
                writer.Write(index);
            }
        }

    }
}
