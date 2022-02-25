using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SugarGuard.Core;
using SugarGuard.Helpers.DynConverter;
using SugarGuard.Helpers.Injection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SugarGuard.Protections
{
    public class Virtualization : Protection
    {
        public override string Name => "Virtualization";
        public override void Execute(Context context)
        {
            context.UpdateModule();

            var module = context.Module;
            var injector = new Injector(module, typeof(Runtime.VMRuntime));
            var executeCall = injector.FindMember("Execute") as MethodDef;
            var initCall = injector.FindMember("Initialize") as MethodDef;

            var cctor = module.GlobalType.FindOrCreateStaticConstructor();

            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            var call = cctor.Body.Instructions[cctor.Body.Instructions.Count - 2].Operand as MethodDef;
            var pos = (int)writer.BaseStream.Position;
            var token = call.MDToken.ToInt32();

            writer.Write(token);
            writer.ConvertToBytes(call);

            SetUpMethod(call, pos, context.Module, executeCall);

            var resourceName = Utils.GenerateString();
            var resource = new EmbeddedResource(resourceName, stream.ToArray(), ManifestResourceAttributes.Public);

            module.Resources.Add(resource);

            var instrs = cctor.Body.Instructions;

            instrs.Insert(instrs.Count - 2, OpCodes.Ldstr.ToInstruction(resourceName));
            instrs.Insert(instrs.Count - 2, OpCodes.Call.ToInstruction(initCall));
            context.Cctor = cctor;
            ControlFlow.ControlFlow.PhaseControlFlow(executeCall, context);
            ControlFlow.ControlFlow.PhaseControlFlow(initCall, context);
            injector.Rename();
        }

        public void SetUpMethod(MethodDef meth, int pos, ModuleDefMD module, MethodDef executeCall)
        {
            var containsOut = false;
            meth.Body.Instructions.Clear();
            var rrr = meth.Parameters.Where(i => i.Type.FullName.EndsWith("&"));
            if (rrr.Count() != 0)
                containsOut = true;

            var rrg = module.CorLibTypes.Object.ToSZArraySig();
            var loc2 = new Local(new SZArraySig(module.CorLibTypes.Object));
            var cli = new CilBody();
            foreach (var bodyVariable in meth.Body.Variables)
                cli.Variables.Add(bodyVariable);
            cli.Variables.Add(loc2);
            var outParams = new List<Local>();
            var testerDictionary = new Dictionary<Parameter, Local>();
            if (containsOut)
                foreach (var parameter in rrr)
                {
                    var locf = new Local(parameter.Type.Next);
                    testerDictionary.Add(parameter, locf);
                    cli.Variables.Add(locf);
                }

            if(meth.Parameters.Count > 0)
            {
                var outp = 0;
                cli.Instructions.Add(new Instruction(OpCodes.Ldc_I4, meth.Parameters.Count));
                cli.Instructions.Add(new Instruction(OpCodes.Newarr, module.CorLibTypes.Object.ToTypeDefOrRef()));
                for (var i = 0; i < meth.Parameters.Count; i++)
                {
                    var par = meth.Parameters[i];
                    cli.Instructions.Add(new Instruction(OpCodes.Dup));
                    cli.Instructions.Add(new Instruction(OpCodes.Ldc_I4, i));
                    if (containsOut)
                        if (rrr.Contains(meth.Parameters[i]))
                        {
                            cli.Instructions.Add(new Instruction(OpCodes.Ldloc, testerDictionary[meth.Parameters[i]]));
                            outp++;
                        }
                        else
                        {
                            cli.Instructions.Add(new Instruction(OpCodes.Ldarg, meth.Parameters[i]));
                        }
                    else
                        cli.Instructions.Add(new Instruction(OpCodes.Ldarg, meth.Parameters[i]));

                    if (true)
                    {
                        cli.Instructions.Add(par.Type.FullName.EndsWith("&")
                            ? new Instruction(OpCodes.Box, par.Type.Next.ToTypeDefOrRef())
                            : new Instruction(OpCodes.Box, par.Type.ToTypeDefOrRef()));
                        cli.Instructions.Add(new Instruction(OpCodes.Stelem_Ref));
                    }
                }
                cli.Instructions.Add(new Instruction(OpCodes.Stloc, loc2));
                cli.Instructions.Add(new Instruction(OpCodes.Ldc_I4, pos));
                cli.Instructions.Add(new Instruction(OpCodes.Ldloc, loc2));
            }
            else
            {
                cli.Instructions.Add(new Instruction(OpCodes.Ldc_I4, pos));
                cli.Instructions.Add(new Instruction(OpCodes.Ldnull));
            }
            cli.Instructions.Add(Instruction.Create(OpCodes.Call, executeCall));
            if (meth.ReturnType.ElementType == ElementType.Void)
                cli.Instructions.Add(Instruction.Create(OpCodes.Pop));
            else if (meth.ReturnType.IsValueType)
                cli.Instructions.Add(Instruction.Create(OpCodes.Unbox_Any, meth.ReturnType.ToTypeDefOrRef()));
            else
                cli.Instructions.Add(Instruction.Create(OpCodes.Castclass, meth.ReturnType.ToTypeDefOrRef()));
            if (containsOut)
            {
                foreach (var parameter in rrr)
                {
                    cli.Instructions.Add(new Instruction(OpCodes.Ldarg, parameter));
                    cli.Instructions.Add(new Instruction(OpCodes.Ldloc, loc2));
                    cli.Instructions.Add(new Instruction(OpCodes.Ldc_I4, meth.Parameters.IndexOf(parameter)));
                    cli.Instructions.Add(new Instruction(OpCodes.Ldelem_Ref));
                    cli.Instructions.Add(new Instruction(OpCodes.Unbox_Any, parameter.Type.Next.ToTypeDefOrRef()));
                    cli.Instructions.Add(new Instruction(OpCodes.Stind_Ref));
                }
                cli.Instructions.Add(new Instruction(OpCodes.Ret));
            }
            else
                cli.Instructions.Add(new Instruction(OpCodes.Ret));
            meth.Body = cli;
            meth.Body.UpdateInstructionOffsets();
            meth.Body.MaxStack += 10;
        }
    }
}
