
using System.Collections.Generic;
using System.IO;
using System.Linq;

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

namespace SugarGuard.Core
{
    public class Context
    {
        public string Path { get; }
        public string OutPutPath { get; }
        public ModuleDefMD Module { get; set; }
        public MethodDef Cctor { get; set; }
        public bool Updated { get; set; }
        public bool HasPacker { get; set; }
        public int VMPos { get; set; }
        public Context(string path)
        {
            Path = path;
            OutPutPath = path.Replace(".exe", "-Sugar.exe");
            Module = ModuleDefMD.Load(path);
            Cctor = Utils.CreateMethod(Module);
            Updated = false;
            HasPacker = false;
            VMPos = -1;
            LoadAssemblies();
        }

        public void UpdateModule()
        {
            Updated = true;
            var realCctor = Module.GlobalType.FindOrCreateStaticConstructor();
            realCctor.Body.Instructions.Insert(realCctor.Body.Instructions.Count - 1, OpCodes.Ldc_I4_M1.ToInstruction());
            realCctor.Body.Instructions.Insert(realCctor.Body.Instructions.Count - 1, OpCodes.Call.ToInstruction(Cctor));
            var stream = new MemoryStream();
            var options = new ModuleWriterOptions(Module);

            options.Logger = DummyLogger.NoThrowInstance;
            options.MetadataOptions.Flags = MetadataFlags.PreserveAll;

            Module.Write(stream, options);

            Module = ModuleDefMD.Load(stream.ToArray());

            LoadAssemblies();
        }

        public byte[] GetBytesToPacker() {
            HasPacker = true;

            var stream = new MemoryStream();
            var options = new ModuleWriterOptions(Module);

            options.Logger = DummyLogger.NoThrowInstance;
            options.MetadataOptions.Flags = MetadataFlags.PreserveAll;

            Module.Write(stream, options);

            var bytes = stream.ToArray();
            Module = ModuleDefMD.Load(bytes);
            return bytes;
        }

        public void LoadAssemblies()
        {
            AssemblyResolver assemblyResolver = new AssemblyResolver();
            ModuleContext moduleContext = new ModuleContext(assemblyResolver);
            assemblyResolver.DefaultModuleContext = moduleContext;
            assemblyResolver.EnableTypeDefCache = true;
            List<AssemblyRef> list = Module.GetAssemblyRefs().ToList();
            Module.Context = moduleContext;

            foreach (AssemblyRef assemblyRef in list)
            {
                bool flag3 = assemblyRef == null;
                if (!flag3)
                {
                    AssemblyDef assemblyDef = assemblyResolver.Resolve(assemblyRef.FullName, Module);
                    bool flag4 = assemblyDef == null;
                    if (!flag4)
                        ((AssemblyResolver)Module.Context.AssemblyResolver).AddToCache(assemblyDef);
                }
            }
        }

        public void SaveFile()
        {

            if (HasPacker)
                return;

            if (!Updated)
            {
                var realCctor = Module.GlobalType.FindOrCreateStaticConstructor();
                realCctor.Body.Instructions.Insert(realCctor.Body.Instructions.Count - 1, OpCodes.Call.ToInstruction(Cctor));
            }
            var options = new ModuleWriterOptions(Module);

            options.Logger = DummyLogger.NoThrowInstance;
            options.MetadataOptions.Flags = MetadataFlags.PreserveAll;

            Module.Write(OutPutPath, options);
        }
    }
}
