// See https://aka.ms/new-console-template for more information

using System.Reflection;
using ModsOfMistriaCommandLine.Lang;

var currentExe = Assembly.GetEntryAssembly();
var currentVersionString =
    currentExe!.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "0.1.0";

Console.WriteLine(Resources.RunningBuild, currentVersionString);

Standalone.Run();

Console.WriteLine(Resources.Completed);
Console.ReadKey();