using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandlebarsDotNet.Mvc
{
	/// <summary>
	/// The different categories used for the logging component in HandlebarsViewEngine. These are the only ones (currently) used by it.
	/// </summary>
	/// <example>
	/// This is what the <see cref="HandlebarsViewEngine.ClearCache"/> method looks like (at one point in time at least):
	/// <code language="C#">
	/// public virtual void ClearCache(ControllerContext controllerContext)
	/// {
	/// 	if(controllerContext == null)
	/// 		throw new ArgumentNullException("controllerContext");
	/// 
	/// 	Logger.Trace(LoggerCategory.Api, () => "ClearCache() called.");
	/// 
	/// 	var globalCacheKey = GetCacheKey(controllerContext, STR_GLOBAL);
	/// 
	/// 	Logger.Log(LoggerCategory.Cache, () => "Clearing the cache.");
	/// 
	/// 	// Simply removing the global cachekey will remove all of it, due to the cache dependencies.
	/// 	Cache.Remove(globalCacheKey);
	/// }
	/// </code>
	/// </example>
	public enum LoggerCategory
	{
		/// <summary>
		/// Category <c>Api</c> is typically used with <see cref="ILogger.Trace"/> just when a method has been entered or about to leave it.
		/// </summary>
		Api,

		/// <summary>
		/// Category <c>Compile</c> is used related to compiling a view, layout, etc.
		/// </summary>
		Compile,

		/// <summary>
		/// Category <c>Cache</c> is used when something related to the caching is logged.
		/// </summary>
		Cache
	}
}
