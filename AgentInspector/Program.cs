using System;
using System.Linq;
using System.Reflection;

var assemblyName = args.Length > 0 ? args[0] : "Microsoft.Agents.AI";
var typeFilter = args.Length > 1 ? args[1] : null;
var assembly = Assembly.Load(assemblyName);
Console.WriteLine($"Loaded {assemblyName}");

Console.WriteLine($"Assembly: {assembly.FullName}");
var types = assembly.GetExportedTypes()
	.Where(t => typeFilter is null || (t.FullName?.Contains(typeFilter, StringComparison.OrdinalIgnoreCase) ?? false))
	.OrderBy(t => t.FullName)
	.ToArray();

foreach (var type in types)
{
	Console.WriteLine(type.FullName);
	Console.WriteLine($"  abstract: {type.IsAbstract}");
	if (type.IsClass)
	{
		foreach (var ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
		{
			Console.WriteLine($"  ctor: {ctor}");
		}
		var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
			.OrderBy(m => m.Name)
			.ToArray();
		foreach (var method in methods)
		{
		Console.WriteLine($"  {method}");
		}
	}
}
