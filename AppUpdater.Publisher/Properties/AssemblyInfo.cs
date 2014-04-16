using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("AppUpdater.Publisher")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("AppUpdater")]
[assembly: AssemblyCopyright("Copyright \u00a9 2012 Diogo Edegar Mafra")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

#if DEBUG
[assembly: AssemblyConfiguration("DEBUG")]
#else
[assembly: AssemblyConfiguration("RELEASE")]
#endif

[assembly: ComVisible(false)]

[assembly: InternalsVisibleTo("AppUpdater.Tests")]
