using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SugarGuard.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SugarGuard.Protections
{
    public class LocalToField : Protection
    {
        public override string Name => "LocalToField";
        public override void Execute(Context context)
        {
            var module = context.Module;
            var cctor = context.Cctor;
            var body = cctor.Body.Instructions;
            foreach (var type in module.GetTypes())
            {
                if (type.IsGlobalModuleType)
                    continue;

                foreach (var method in type.Methods)
                {
                    if (!method.HasBody || !method.Body.HasInstructions)
                        continue;

                    var instrs = method.Body.Instructions;

                    if (!instrs.Any(x => x.IsLdcI4()))
                        continue;

                    var first = instrs.First(x => x.IsLdcI4());

                    var value = first.GetLdcI4Value();

                    var field = Utils.CreateField(new FieldSig(module.CorLibTypes.Int32));
                    module.GlobalType.Fields.Add(field);

                    body.Insert(0, OpCodes.Ldc_I4.ToInstruction(value));
                    body.Insert(1, OpCodes.Stsfld.ToInstruction(field));

                    first.OpCode = OpCodes.Ldsfld;
                    first.Operand = field;
                }
            }
        }
    }
}
