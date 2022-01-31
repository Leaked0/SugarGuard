using dnlib.DotNet.Emit;

namespace SugarGuard.Helpers.Emulator.Instructions {
    internal class Sub : EmuInstruction {
        internal override OpCode OpCode => OpCodes.Sub;

        internal override void Emulate(EmuContext context, Instruction instr) {
            var right = (int)context.Stack.Pop();
            var left = (int)context.Stack.Pop();

            context.Stack.Push(left - right);
        }
    }
}
