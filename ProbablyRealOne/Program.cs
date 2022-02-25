
using SugarGuard.Core;
using SugarGuard.Protections;
using SugarGuard.Protections.ControlFlow;

using System;
using System.Reflection;

namespace SugarGuard
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = args[0];
            var context = new Context(path);
            var protections = new Protection[]
            {
                new ImportProtection(),
                new ControlFlow(),
                new LocalToField(),
                new Virtualization(),
                new RenamerProtection()
            };

            foreach (var protection in protections)
                protection.Execute(context);

            context.SaveFile();
        }
    }
}
