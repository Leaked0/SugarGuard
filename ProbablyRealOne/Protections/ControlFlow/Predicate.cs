using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;

namespace SugarGuard.Protections.ControlFlow
{
    internal interface IPredicate
    {
        void Init(CilBody body);
        void EmitSwitchLoad(IList<Instruction> instrs);
        int GetSwitchKey(int key);
    }

    internal class Predicate : IPredicate
    {
        readonly ModuleDefMD ctx;
        bool inited;
        int xorKey;

        public Predicate(ModuleDefMD ctx)
        {
            this.ctx = ctx;
        }

        public void Init(CilBody body)
        {
            if (inited)
                return;

            xorKey = new Random().Next();
            inited = true;
        }

        public void EmitSwitchLoad(IList<Instruction> instrs)
        {
            instrs.Add(Instruction.Create(OpCodes.Ldc_I4, xorKey));
            instrs.Add(Instruction.Create(OpCodes.Xor));
        }

        public int GetSwitchKey(int key)
        {
            return key ^ xorKey;
        }
    }
}
