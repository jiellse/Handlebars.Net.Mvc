using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandlebarsDotNet.Mvc
{
	internal class FileDoesntExist
	{
		// Empty class, just a marker in the cache.
		// Used by HandleBarsViewEngine.FileExists to not have to check the file system for the same file over and over.
		// Also used when there was a problem compiling a view - it will be treated as missing until it can be compiled.

		internal static FileDoesntExist Instance = new FileDoesntExist();
	}
}
