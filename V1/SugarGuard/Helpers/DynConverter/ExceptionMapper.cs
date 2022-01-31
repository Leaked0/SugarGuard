using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SugarGuard.Helpers.DynConverter
{
    public class ExceptionMapper
    {
        private IList<ExceptionHandler> Exceptions { get; }
        public ExceptionMapper(MethodDef method)
        {
            Exceptions = method.Body.ExceptionHandlers;
        }

        public void MapAndWrite(BinaryWriter writer, Instruction instr) 
        {
            var count = 0;
            var list = new List<int>();
            foreach (var exception in Exceptions)
            {
                if (exception.TryStart == instr)
                {
                    list.Add(0);
                    count++;
                    continue;
                }

                if (exception.HandlerEnd == instr)
                {
                    list.Add(5);
                    count++;
                    continue;
                }

                if (exception.HandlerType == ExceptionHandlerType.Filter)
                {
                    if (exception.FilterStart == instr)
                    {
                        list.Add(1);
                        count++;
                        continue;
                    }
                }

                if (exception.HandlerStart == instr)
                {
                    switch (exception.HandlerType)
                    {
                        case ExceptionHandlerType.Catch:
                            list.Add(2);
                            if (exception.CatchType == null)
                                list.Add(-1);
                            else
                                list.Add(exception.CatchType.MDToken.ToInt32());
                            break;
                        case ExceptionHandlerType.Finally:
                            list.Add(3);
                            break;
                        case ExceptionHandlerType.Fault:
                            list.Add(4);
                            break;
                    }
                    count++;
                    continue;
                }


            }
            writer.Write(count);
            foreach (var i in list)
                writer.Write(i);
        }
    }
}
