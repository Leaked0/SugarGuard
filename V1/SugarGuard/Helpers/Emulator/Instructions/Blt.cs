using dnlib.DotNet.Emit;

namespace SugarGuard.Helpers.Emulator.Instructions {
    internal class Blt : EmuInstruction {
        internal override OpCode OpCode => OpCodes.Blt;

        internal override void Emulate(EmuContext context, Instruction instr) {
            var right = (int)context.Stack.Pop();
            var left = (int)context.Stack.Pop();

            if (left < right) {
                context.InstructionPointer = context.Instructions.IndexOf((Instruction)instr.Operand);
            }
        }
    }
}
