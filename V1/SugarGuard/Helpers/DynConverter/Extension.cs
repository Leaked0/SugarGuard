using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SugarGuard.Helpers.DynConverter
{
    public static class Extension
    {
        public static void ConvertToBytes(this BinaryWriter writer, MethodDef method) => new Converter(method, writer).ConvertToBytes();
    }
}
