using System;
using System.Linq;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace SugarGuard.Helpers.Emulator{
    public class Emulator {
        public Emulator(List<Instruction> instructions, List<Local> locals) {

            this._context = new EmuContext(instructions, locals);

            this._emuInstructions = new Dictionary<OpCode, EmuInstruction>();

            var emuInstructions = typeof(EmuInstruction).Assembly
                .GetTypes()
                .Where(t => t.IsSubclassOf(typeof(EmuInstruction)) && !t.IsAbstract)
                .Select(t => (EmuInstruction)Activator.CreateInstance(t))
                .ToList();

            foreach (var instrEmu in emuInstructions) {
                this._emuInstructions.Add(instrEmu.OpCode, instrEmu);
            }
        }

        internal int Emulate() {

            for (int i = _context.InstructionPointer; i < _context.Instructions.Count; i++)
            {
                var current = _context.Instructions[i];
                if (current.OpCode == OpCodes.Stloc_S)
                    break;
                if (current.OpCode != OpCodes.Nop)
                    _emuInstructions[current.OpCode].Emulate(_context, current);
            }

            return (int)_context.Stack.Pop();
        }

        public EmuContext _context;

        private Dictionary<OpCode, EmuInstruction> _emuInstructions;
    }
}
