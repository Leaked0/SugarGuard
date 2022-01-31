using dnlib.DotNet.Emit;

namespace SugarGuard.Helpers.Emulator.Instructions {
    internal class Ldloc : EmuInstruction {
        internal override OpCode OpCode => OpCodes.Ldloc_S;

        internal override void Emulate(EmuContext context, Instruction instr) {
            context.Stack.Push(context.GetLocalValue((Local)instr.Operand));
        }
    }
}
