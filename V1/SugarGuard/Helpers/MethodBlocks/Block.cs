using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SugarGuard.Helpers.MethodBlocks
{
    public class Block
    {
        public List<Instruction> Instructions;
        public bool IsSafe;
        public bool IsBranched;
        public bool IsException;
        public List<int> Values;
        public Block(Instruction[] Instructions, bool IsException = false, bool IsSafe = true, bool IsBranched = false, int initValue = -1)
        {
            this.Instructions = Instructions.ToList();
            this.IsSafe = IsSafe;
            this.IsBranched = IsBranched;
            this.IsException = IsException;
            Values = new List<int>() { initValue };
        }

        public static Block Clone(Block block, bool all = false)
        {
            var instructions = new List<Instruction>();

            foreach (var instr in block.Instructions)
            {
                var newInstr = new Instruction();
                newInstr.OpCode = instr.OpCode;
                newInstr.Operand = instr.Operand;
                instructions.Add(newInstr);

                if (!all && instr.OpCode == OpCodes.Stloc_S)
                    break;
            }

            return new Block(instructions.ToArray(), block.IsException, block.IsSafe, block.IsBranched);
        }

    }
}
