using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dnlib.DotNet;
using SugarGuard.Core;

namespace SugarGuard.Protections
{
    public class RenamerProtection : Protection
    {
        public override string Name => "Renamer";

        public override void Execute(Context context) {

            foreach (var type in context.Module.GetTypes())
                foreach (var method in type.Methods)
                {
                    if (!method.HasBody)
                        continue;
                    if (!method.Body.HasInstructions)
                        continue;

                    foreach (var paramDef in method.ParamDefs)
                        paramDef.Name = GenerateString();
                    foreach (var local in method.Body.Variables)
                        local.Name = GenerateString();

                    var declType = method.DeclaringType;

                    if (CanRename(declType, method))
                        method.Name = GenerateString();
                    if (CanRename(declType))
                        declType.Name = GenerateString();
                    if (!declType.Namespace.StartsWith("<"))
                        declType.Namespace = GenerateString();

                    foreach (var field in declType.Fields)
                        if (CanRename(field))
                            field.Name = GenerateString();
                }
        }

        public static Random rnd = new Random();
        public static string GenerateString()
        {
            int seed = rnd.Next();
            return (seed * 0x19660D + 0x3C6EF35).ToString("X");
        }

        public static bool CanRename(FieldDef field)
        {
            if (field.IsSpecialName)
                return false;
            if (field.IsRuntimeSpecialName)
                return false;
            return true;
        }

        public static bool CanRename(TypeDef declType)
        {
            if (declType.IsGlobalModuleType)
                return false;
            if (declType.IsInterface)
                return false;
            if (declType.IsAbstract)
                return false;
            if (declType.IsEnum)
                return false;
            if (declType.IsRuntimeSpecialName)
                return false;
            if (declType.IsSpecialName)
                return false;
            return true;
        }

        public static bool CanRename(TypeDef declType, MethodDef method)
        {
            if (declType.IsInterface)
                return false;
            if (declType.IsDelegate || declType.IsAbstract)
                return false;

            if (method.IsSetter || method.IsGetter)
                return false;
            if (method.IsSpecialName || method.IsRuntimeSpecialName)
                return false;
            if (method.IsConstructor)
                return false;
            if (method.IsVirtual)
                return false;
            if (method.IsNative)
                return false;
            if (method.IsPinvokeImpl || method.IsUnmanaged || method.IsUnmanagedExport)
                return false;
            if (method.HasImplMap)
                return false;

            return true;
        }
    }
}
