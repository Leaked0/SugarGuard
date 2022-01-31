using dnlib.DotNet.Emit;

namespace SugarGuard.Helpers.Emulator
{
    internal abstract class EmuInstruction {
        internal abstract OpCode OpCode { get; }

        internal abstract void Emulate(EmuContext context, Instruction instr);
    }
}
