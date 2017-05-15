using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("HandlebarsDotNet.Mvc")]
[assembly: AssemblyDescription("An ASP.NET MVC ViewEngine using the Handlebars syntax")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Jiell")]
[assembly: AssemblyProduct("HandlebarsDotNet.Mvc")]
[assembly: AssemblyCopyright("Copyright © Jiell 2017")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("af2d2325-66ac-42c0-8b10-c4a856c32390")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]

// http://stackoverflow.com/questions/11965924/nuspec-version-attribute-vs-assembly-version
// http://stackoverflow.com/questions/64602/what-are-differences-between-assemblyversion-assemblyfileversion-and-assemblyin

[assembly: AssemblyVersion("0.1")]
// The public API version. If the public surface area changes, change this.
// This is the version other code binds to. (If this changes old code needs a binding redirect, but that may break the old code.)

[assembly: AssemblyFileVersion("0.1")]
// The specific assembly build.

[assembly: AssemblyInformationalVersion("0.1.0-alpha")]
// The human-readable variant of the file version. The version is a string that doesn't need to be parseable by System.Version.


[assembly: CLSCompliant(true)]	// Unfortunately HandlebarsDotNet is not CLS-compliant so [CLSCompliant(false)] is used wherever a type from that assembly is used

[assembly: SecurityRules(SecurityRuleSet.Level2)]

[assembly: InternalsVisibleTo("HandlebarsDotNet.Mvc.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
