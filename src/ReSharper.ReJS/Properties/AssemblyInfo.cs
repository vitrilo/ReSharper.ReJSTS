using System.Reflection;
using JetBrains.ActionManagement;
#if !RESHARPER9
using JetBrains.Application.PluginSupport;
#endif
// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CSharp.Errors;
using ReSharper.ReJS;

[assembly: AssemblyTitle("ReSharper.ReJSTS")]
[assembly: AssemblyDescription("Useful refactorings for JavaScript and Typescript")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Alexander Zaytsev, Anton Bogdan")]
[assembly: AssemblyProduct("ReSharper.ReJSTs")]
[assembly: AssemblyCopyright("Copyright © Alexander Zaytsev, Anton Bogdan 2013-2016")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: AssemblyVersion("0.6.0.0")]
[assembly: AssemblyFileVersion("0.6.0.0")]

#if !RESHARPER9
// The following information is displayed by ReSharper in the Plugins dialog
[assembly: PluginTitle("ReSharper.ReJS-WithTS")]
[assembly: PluginDescription("Useful refactorings for JavaScript and Typescript")]
[assembly: PluginVendor("Alexander Zaytsev, Anton Bogdan 2013-2016")]
#endif

//[assembly: RegisterConfigurableSeverity(AccessToModifiedClosureWarning.HIGHLIGHTING_ID, null, HighlightingGroupIds.CodeSmell, "Access to modified closure", "\n          Access to closure variable from anonymous function when the variable is modified externally\n        ", Severity.WARNING, false)]
//[assembly: RegisterConfigurableSeverity(CallWithSameContextWarning.HIGHLIGHTING_ID, null, HighlightingGroupIds.CodeRedundancy, "Call of a function with the same context", "Call of a function with the same context", Severity.WARNING, false)]

