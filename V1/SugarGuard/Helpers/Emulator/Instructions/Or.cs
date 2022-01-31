using dnlib.DotNet.Emit;

namespace SugarGuard.Helpers.Emulator.Instructions {
    internal class Or : EmuInstruction {
        internal override OpCode OpCode => OpCodes.Or;

        internal override void Emulate(EmuContext context, Instruction instr) {
            var right = (int)context.Stack.Pop();
            var left = (int)context.Stack.Pop();

            context.Stack.Push(left | right);
        }
    }
}