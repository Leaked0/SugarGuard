using dnlib.DotNet;
using SugarGuard.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SugarGuard.Helpers.Injection
{
    public class Injector
    {
        public ModuleDefMD TargetModule { get; }
        public Type RuntimeType { get; }
        public List<IDnlibDef> Members { get; }
        public Injector(ModuleDefMD targetModule, Type type, bool injectType = true)
        {
            TargetModule = targetModule;
            RuntimeType = type;
            Members = new List<IDnlibDef>();

            if (injectType)
                InjectType();
        }

        public void InjectType()
        {
            var typeModule = ModuleDefMD.Load(RuntimeType.Module);
            var typeDefs = typeModule.ResolveTypeDef(MDToken.ToRID(RuntimeType.MetadataToken));
            Members.AddRange(InjectHelper.Inject(typeDefs, TargetModule.GlobalType, TargetModule).ToList());
        }
        public IDnlibDef FindMember(string name)
        {
            foreach (var member in Members)
                if (member.Name == name)
                    return member;
            throw new Exception("Error to find member.");
        }

        public void Rename()
        {
            foreach (var mem in Members)
            {
                if (mem is MethodDef method)
                {
                    if (method.HasImplMap)
                        continue;
                    if (method.DeclaringType.IsDelegate)
                        continue;
                }
                mem.Name = Utils.GenerateString();
            }
        }
    }
}
