using System;
using SugarGuard.Core;
using SugarGuard.Protections.Mutation;

namespace SugarGuard
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			string path = args[0];
			Context context = new Context(path);
			Protection[] protections = new Protection[]
			{
				new MutationConfusion()
			};
			foreach (Protection protection in protections)
			{
				protection.Execute(context);
			}
			context.SaveFile();
		}
	}
}
