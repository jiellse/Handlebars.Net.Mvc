using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Routing;

// This ViewEngine for ASP.NET MVC is for using the Handlebars syntax in its views.
// Under the hood it uses HandlebarsDotNet for compiling the views in order to not go through a Javascript compiler.
// Special care has been taken for it to feel like any other view engine.
//
// Some design decissions have been made to make it application cache friendly (for example layout files in a separate directory so updates to a layout file doesn't invalidate the view cache because the dir is updated).
//
// Default file extension: ".hbs"
//
// (The underscore is there so when listing directories they will come first, and also not be mixed with controller names.)
// ~/Views/_Layouts/{0}.hbs
// ~/Views/_Partials/{0}.hbs

// https://github.com/rexm/Handlebars.Net
// http://handlebarsjs.com/
// https://github.com/barc/express-hbs
// http://themes.ghost.org/docs/handlebars

// When doing "return PartialView()" in a controller the layout will not be rendered, only the view (obviously).
// If the view designated some content for a content block it will not be rendered. One way to get the missing content
// could be to render the view with a custom layout which doesn't render the view's default content but only the desired content block.
// If the client need both, for example to get new content to replace some fragment in an AJAX fashion but also need to get the required script files for that content,
// this could be handled by a new controller function AjaxPartial (as an example) for the controller that returns both in a single render pass.

// About the cache keys:
// This is just to document what this implementation uses _at the moment_. It is not to be relied upon for users of this package.
//
//	".hbsVE:global"
//		This doesn't have any data associated with it. Instead all other cache entries have a dependency on this one, meaning to flush the cache all I need to do is remove this entry.
//		(Direct dependency for file missing entry and cached configuration, indirect for view files that has dependency on the config which has the dependency on the global one.)
//		Needed if the HandlebarsViewEngine.HandlebarsConfiguration that is used as a template is changed (by adding global helpers or partials while running).
//
//	".hbsVE:area:controller:config"
//		The data for this one is an IHandlebars. When the compiled views are run they can call helpers and partials that have been registered in this configuration.
//		The file partials found for this area+controller combination is compiled and registered in the config.
//		Has dependencies on the file partials found and the folder paths where partials can be found. No expiration.
//
//	".hbsVE:area:controller:config00000000000000000000000000000000" (a Guid at the end - the cachekey is only used for inserting into the cache, retrieval is not necessary)
//		This entry only exists if the VirtualPathProvider doesn't support CacheDependency.
//		The data for this one is a class that contains the filepaths, filehashes and folder paths for partials. If any changes then the config is removed from the cache.
//		It uses a CacheItemUpdateCallback to recheck every X seconds.
//
//	".hbsVE:area:controller:~/Views/Home/Index.hbs"
//		The data for this one is either a FileIsMissing or a CompiledView. If the cache entry doesn't exist we need to check for file existance.
//		Has a dependency on the config and the source file. Sliding expiration.

// Microsoft "recommends" using System.Runtime.Caching (ObjectCache) instead of System.Web.Caching (Cache). The latter one is for web applications and have existed since ASP.NET 1.1,
// the newer System.Runtime.Caching is for all application types and was added in .NET Framework version 4. Unfortunately the new one doesn't allow the CacheDependency from the old one that
// can be gotten from the VirtualPathProvider. With us being a building block for a web application this doesn't really concern us, but clients may find a need to bridge the two implementations.
// If you find yourself in that situation the following links can be helpful:
// http://stackoverflow.com/questions/23129913/httpcache-and-memorycache-cross-dependency
// http://codereview.stackexchange.com/questions/47488/httpcache-and-memorycache-cross-dependency

// TODO: Add logging just about everywhere

namespace HandlebarsDotNet.Mvc
{
	/// <summary>
	/// An ASP.NET MVC ViewEngine that uses the Handlebars syntax for its view files.
	/// </summary>
	public class HandlebarsViewEngine : IViewEngine
	{
		/// <summary>
		/// Initializes a new instance of the <c>HandlebarsViewEngine</c> class with <see cref="DefaultPathsProvider"/> as its path provider.
		/// </summary>
		public HandlebarsViewEngine()
			: this(new DefaultPathsProvider())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <c>HandlebarsViewEngine</c> class.
		/// </summary>
		/// <param name="pathsProvider">The paths provider.</param>
		public HandlebarsViewEngine(IPathsProvider pathsProvider)
		{
			if(pathsProvider == null)
			{
				throw new ArgumentNullException("pathsProvider");
			}

			PathsProvider = pathsProvider;
		}

		/// <summary>
		/// A copy of this will be used when compiling the views.
		/// This is the place to provide site-wide helpers, block-helpers and templates.
		/// </summary>
		[CLSCompliant(false)]
		public virtual HandlebarsConfiguration HandlebarsConfiguration
		{
			get { return _handlebarsconfiguration; }
			protected set { _handlebarsconfiguration = value; }
		}
		private HandlebarsConfiguration _handlebarsconfiguration = new HandlebarsConfiguration();

		/// <summary>The length of time that compiled views are cached after last access. Set this to <see langword="null"/> to disable caching. Default is 5 minutes.</summary>
		/// <remarks>If the VirtualPathProvider supports cache dependencies then an update to the source file will force a recompilation of the view.</remarks>
		public virtual TimeSpan ViewsSlidingCacheTime
		{
			get { return _viewsslidingcachetime; }
			set { _viewsslidingcachetime = value; }
		}
		private TimeSpan _viewsslidingcachetime = TimeSpan.FromMinutes(5);

#if false
		/// <summary>The length of time that compiled templates ("partials" in Handlebars lingo) are cached after last access. Set this to null to disable caching. Default is 15 minutes.</summary>
		/// <remarks>
		/// If the VirtualPathProvider supports cache dependencies then an update to the source file will force a recompilation of the template.
		/// The length of time should be larger than ViewsSlidingCacheTime.
		/// </remarks>
		public TimeSpan TemplatesSlidingCacheTime
		{
			get { return _templatesslidingcachetime; }
			set { _templatesslidingcachetime = value; }
		}
		private TimeSpan _templatesslidingcachetime = TimeSpan.FromMinutes(15);
#endif

		/// <summary>
		/// The paths provider used. The default is <see cref="DefaultPathsProvider"/> that mostly mimics the view files placements in the built-in MVC framework.
		/// </summary>
		public IPathsProvider PathsProvider { get; private set; }
		// Not "virtual". Only settable through the constructor.

		/// <summary>The file extension the view files and partials must have in order for this ViewEngine to handle it. Default: ".hbs"</summary>
		/// <remarks>Please note this should include the dot (contrary to VirtualPathProviderViewEngine).</remarks>
		public virtual string ViewExtension
		{
			get { return _viewextension; }
			set { _viewextension = value;}
		}
		private string _viewextension = ".hbs";

		/// <summary>
		/// The logger used. The default is a <see cref="NullLogger"/> instance.
		/// </summary>
		public virtual ILogger Logger
		{
			get { return _logger; }
			set { _logger = value ?? NullLogger.Instance; }
		}
		private ILogger _logger = NullLogger.Instance;

		/// <summary>
		/// The VirtualPathProvider to use. This is only to facilitate unit testing! The default is the registered VirtualPathProvider.
		/// </summary>
		protected internal VirtualPathProvider VirtualPathProvider
		{
			get { return _vppFunc(); }
			internal set
			{
				if(value == null)
					throw new ArgumentNullException("value");
				_vppFunc = () => value;
			}
		}
		private Func<VirtualPathProvider> _vppFunc = () => HostingEnvironment.VirtualPathProvider;

		internal ICache Cache
		{
			get { return _cache; }
			set
			{
				if(value == null)
					throw new ArgumentNullException("value");
				_cache = value;
			}
		}
		private ICache _cache = new DefaultCache();

		/// <summary>
		/// The prefix to use when creating the cache keys.
		/// </summary>
		/// <remarks>
		/// If you against all odds need to register multiple instances of <see cref="HandlebarsViewEngine"/> in <see cref="ViewEngines.Engines"/> then each instance must have its own unique <c>CacheKeyPrefix</c>.<br />
		/// The default prefix is a random value so usually this doesn't need to be set explicitly.
		/// </remarks>
		public virtual string CacheKeyPrefix
		{
			get { return _cachekeyprefix; }
			set { _cachekeyprefix = value; }
		}
		private string _cachekeyprefix = Guid.NewGuid().ToString();	//".hbsVE";
		// "public" so it can be set by clients.
		// GetCacheKey() is "protected" and not "public" because it's not supposed to be called by clients.

		private const string STR_GLOBAL = "global";
		private const string STR_CONFIG = "config";

		// 'path' should be in the form "~/Views/Home/Index.hbs"
		// 'path' can also represent a directory (ending in "/")
		// 'path' can also be "config" to get the cache key for the IHandlebars for this area+controller combination
		// 'path' can also be "global" to get the global cache key.
		/// <summary>
		/// Gets the cachekey for the specified path.
		/// </summary>
		/// <param name="controllerContext">The controller context.</param>
		/// <param name="path">The path to create the cachekey for, usually in the form "~/Views/Home/Index.hbs". Can also be either of two (currently) special values: "global" for the global cachekey, and "config" for the cachekey used for this combination of area+controller.</param>
		/// <returns>The cache key to use for the specified path.</returns>
		/// <remarks>
		/// This can be overridden in a subclass to for example provide tenant-specific cachekeys.
		/// </remarks>
		protected virtual string GetCacheKey(ControllerContext controllerContext, string path)
		{
			if(controllerContext == null)
				throw new ArgumentNullException("controllerContext");

			if(string.IsNullOrEmpty(path))
				throw new ArgumentException("Argument cannot be null or empty.", "path");

			Logger.Trace(LoggerCategory.Api, () => string.Format("GetCacheKey() called for path=\"{0}\".", path));

			if(path == STR_GLOBAL)
			{
				string cachekey = CacheKeyPrefix + ":" + STR_GLOBAL;

				// Make sure the global one always exists, so a CacheDependency on it doesn't become expired simply because it is created.
				if(Cache.Get(cachekey) == null)
				{
					CreateGlobalCacheEntry(cachekey);
				}

				return cachekey;
			}
			else
			{
				// This code path also works for the case where path is "config"

				string area       = ExtractAreaName(controllerContext) ?? string.Empty;
				string controller = ExtractControllerName(controllerContext);

				// Example: ".hbsVE::Home:~/Views/Home/Index.hbs"
				return CacheKeyPrefix + ":" + area + ":" + controller + ":" + path;
			}
		}

		/// <summary>
		/// Clears the cache used by HandlebarsViewEngine.
		/// </summary>
		/// <param name="controllerContext">The controller context.</param>
		/// <remarks>
		/// If the <see cref="HandlebarsConfiguration"/> is changed after this <see cref="HandlebarsViewEngine"/> was added to <see cref="ViewEngines.Engines"/> this need to be called.
		/// </remarks>
		public virtual void ClearCache(ControllerContext controllerContext)
		{
			if(controllerContext == null)
				throw new ArgumentNullException("controllerContext");

			Logger.Trace(LoggerCategory.Api, () => "ClearCache() called.");

			var globalCacheKey = GetCacheKey(controllerContext, STR_GLOBAL);

			Logger.Log(LoggerCategory.Cache, () => "Clearing the cache.");

			// Simply removing the global cachekey will remove all of it, due to the cache dependencies.
			Cache.Remove(globalCacheKey);

			// Create it immediately
			CreateGlobalCacheEntry(globalCacheKey);
		}

		private void CreateGlobalCacheEntry(string cachekey)
		{
			Cache.Insert(cachekey, string.Empty, null);
		}

		/// <summary>
		/// Checks for file existence.
		/// </summary>
		/// <param name="controllerContext">The controller context.</param>
		/// <param name="virtualPath">The path to check the existence for.</param>
		/// <returns><see langword="true"/> if the file exists, <see langword="false"/> if it doesn't.</returns>
		protected virtual bool FileExists(ControllerContext controllerContext, string virtualPath)
		{
			// This function checks the cache whether the file exists.
			// If an entry for that virtualpath isn't in the cache the VirtualPathProvider is used for checking the file existence.
			// If it doesn't exist there, we remember that fact by adding to the cache.
			// If the VPP says the file exists (but it wasn't in the cache otherwise we wouldn't have asked the VPP) then it _doesn't_ add to the cache.
			// Instead when the HandlebarsViewEngince tries to get the CompiledView for that virtualpath it won't get it, and will therefor compile the file and add a CompiledView to the cache.

			Logger.Trace(LoggerCategory.Api, () => string.Format("Entering FileExists() for path=\"{0}\".", virtualPath));

			string cachekey = GetCacheKey(controllerContext, virtualPath);

			object o = Cache.Get(cachekey);

			if(o == null)
			{
				DateTime utcstart = DateTime.UtcNow;  // Grab the time now (before the existence check) so we don't miss it if it is added between now and when we create the CacheDependency

				bool exists = VirtualPathProvider.FileExists(virtualPath);

				if(!exists)
				{
					// Doesn't exist (yet) so remember this fact.

					o = FileDoesntExist.Instance;

					CacheDependency dependency = null;
					try
					{
						// If the directory doesn't exist an exception is thrown, despite the documentation saying it should work.
						// System.Web.HttpException: Directory '(path here)' does not exist. Failed to start monitoring file changes.
						dependency = VirtualPathProvider.GetCacheDependency(virtualPath, virtualPathDependencies: new string[] { virtualPath }, utcStart: utcstart);
					}
					catch(HttpException)
					{
					}

					var configcachekey = new string[] { GetCacheKey(controllerContext, STR_CONFIG) };

					// If the VPP supports CacheDependency, use a sliding expiration. If it doesn't, use absolute expiration instead.
					if(dependency != null)
					{
						// The first argument to this constructor is an array of filenames.
						// Because the VirtualPathProvider was nice enough to give us a CacheDependency object we are going to assume
						// it will expire when the file is created, so use a long sliding time for cache removal.

						dependency = new CacheDependency(null, configcachekey, dependency);	// make it also dependent on the config cachekey

						Logger.Trace(LoggerCategory.Cache, () => string.Format("Adding filemissing to cache with 15 minutes sliding expiration"));

						Cache.Insert(cachekey, o, dependency, Cache.NoAbsoluteExpiration, TimeSpan.FromMinutes(15));
					}
					else
					{
						// The first argument to this constructor is an array of filenames.
						// Because we are using a VirtualPathProvider we can't tell if it is a local path so we are a little bit out of luck
						// and can only expire this cache entry (file missing) relatively quickly.

						dependency = new CacheDependency(null, configcachekey); // make it dependent on the config cachekey

						Logger.Trace(LoggerCategory.Cache, () => string.Format("Adding filemissing to cache; will be removed in 3 seconds"));

						Cache.Insert(cachekey, o, dependency, utcstart.AddSeconds(3), Cache.NoSlidingExpiration);
					}
				}
				else
				{
					// Do not add to the cache here: HandlebarsViewEngine will do it soon enough with the compiled source.
				}

				Logger.Trace(LoggerCategory.Api, () => string.Format("Leaving FileExists() for path=\"{0}\": exists={1}", virtualPath, exists));
				return exists;
			}

			Logger.Trace(LoggerCategory.Api, () => string.Format("Leaving FileExists() for path=\"{0}\": exists={1}", virtualPath, !(o is FileDoesntExist)));
			return !(o is FileDoesntExist);
		}

		/// <summary>
		/// Creates an <see cref="IView"/> instance that is ready to be rendered.
		/// </summary>
		/// <param name="controllerContext">The controller context.</param>
		/// <param name="compiledView">The compiled view. The <c>compiledView.Layout</c> property is not used.</param>
		/// <param name="layouts">The compiled layouts that will surround the view.</param>
		/// <returns>An <c>IView</c> instance.</returns>
		/// <remarks>
		/// The default implementation simply creates a <see cref="HandlebarsView"/> using the arguments provided.
		/// </remarks>
		protected virtual IView CreateView(ControllerContext controllerContext, CompiledView compiledView, CompiledView[] layouts)
		{
			return new HandlebarsView(controllerContext, compiledView, layouts);
		}

		// If you ask me the whole idea with 'useCache' is not well-thought out / fully baked / plain confusing.
		// Whether to use a cache should be an implementation detail. We should only be called one time and give a definite answer right away.
		//
		/* From the comments to http://www.hanselman.com/blog/ABetterASPNETMVCMobileDeviceCapabilitiesViewEngine.aspx:
		 *
		 * Mike:
		 *		Question: Why does FindView take a flag for "useCache"? You'd think that caching/not caching would be hidden to callers, and that the base FindView implementation would handle this completely behind the scenes. 
		 *
		 * Hanselman:
		 *		Michael, the MVC team says this:
		 *			Kind of an odd pattern to be sure. Remember that the view engine is a black box to the MVC pipeline; MVC doesn’t know what the engine is looking at.
		 *			Think of it this way – if useCache is true, then MVC wants you to give an answer as quickly as possible, and it’s willing to tolerate a false negative.
		 *			If useCache is false, then MVC wants you to give a correct answer, and it’s willing to wait the extra time for you to generate that answer.
		 *			The built-in view engines will populate the cache when useCache is false so that subsequent lookups (with useCache = true) are very fast.
		 */
		// Also see this: http://stackoverflow.com/questions/2399180/asp-net-mvc-view-engine-resolution-sequence
		//
		// MVC finds views by calling ViewEngines.Engines.FindView()/FindPartialView()
		// https://github.com/ASP-NET-MVC/aspnetwebstack/blob/master/src/System.Web.Mvc/ViewEngineCollection.cs
		// On first call useCache=true. The most common viewengines are derived from VirtualPathProvider and that one just checks if a previous cache-hit was found.
		// Consider you have two viewengines installed and both can be successful.
		// In the first pass the first viewengine doesn't have it in its cache (whatever the cache is) so it returns null.
		// The second viewengine finds it and returns it. Fail! The first one should have found it.
		// One example of the second type is a fictional EmbeddedResourcesViewEngine that loads all the resources as part of its initialization.
		//
		// It works if all implementations use the same cache or the same check or only handles different paths, but how likely is all of this?
		// On top of that we don't know if the behavior from ViewEngine.Engines.FindView()/FindPartialView() is even used. Maybe the application uses a different ControllerActionInvoker
		// or some other part has been exchanged - there are a lot of extensibility points in ASP.NET MVC after all.
		//
		// If no view was found from any the viewengines MVC will show the Yellow-screen-of-death with the paths that were searched. Those paths only comes from the pass with useCache=false,
		// the paths from the 'true' case are thrown away. In the source for the ViewEnginesCollection there are different passes with a 'trackSearchedPaths' that is directly related to 'useCache'.
		// The sad part is we cannot rely on this fact because we don't know if the application has changed the ControllerActionInvoker or if there are other view engines that has a different
		// opinion on what cache it should check, etc.
		//
		// The only sensible way to implement this is that both passes should really try to find the view.
		// As we don't know who called us we should always return the searched paths if we couldn't find the view, regardless of the value for 'useCache'.
		//
		// This also means checking for file existence for all paths. This is probably what the "useCache" is trying to prevent, but as mentioned above it can't be relied upon.
		// Doing it the correct way would break other view engines that are relying on "the observed way" that are registered before this one.
		// I may implement a setting to control whether you want "the observed default VirtualPathProviderViewEngine" behavior or the documented behavior - either you want the speedier way or the correct way.
		// Mode.Conformant|Performant

		// Another problem with VirtualPathProviderViewEngine (I think, haven't checked) is if the view path has been cached, subsequent cache hits don't check if the file is still present.

		/// <summary>
		/// Finds the specified partial view by using the specified controller context.
		/// </summary>
		/// <param name="controllerContext">The controller context.</param>
		/// <param name="partialViewName">The name of the partial view.</param>
		/// <param name="useCache"><see langword="true"/> to specify that the view engine returns the cached view, if a cached view exists; otherwise, <see langword="false"/>.</param>
		/// <returns>The partial view.</returns>
		public virtual ViewEngineResult FindPartialView(ControllerContext controllerContext, string partialViewName, bool useCache)
		{
			if(controllerContext == null)
				throw new ArgumentNullException("controllerContext");

			if(string.IsNullOrEmpty(partialViewName))
				throw new ArgumentNullException("partialViewName");

			Logger.Trace(LoggerCategory.Api, () => string.Format("Entering FindPartialView(): partialView={0}, useCache={1}.",
				partialViewName != null ? "\"" + partialViewName + "\"" : "(null)",
				useCache));

			IEnumerable<string> paths = PathsProvider.GetViewFolders(controllerContext);
			List<string> searchedPaths = new List<string>();

			var compiledView = GetCompiledView(controllerContext, partialViewName, paths, ref searchedPaths);

			if(compiledView != null)
			{
				Logger.Trace(LoggerCategory.Api, () => string.Format("Leaving FindPartialView(): view was found."));

				return FoundView(CreateView(controllerContext, compiledView, null), this);
			}

			Logger.Trace(LoggerCategory.Api, () => string.Format("Leaving FindPartialView(): view not found."));
			return ViewNotFound(searchedPaths);
		}

		/// <summary>
		/// Finds the specified view by using the specified controller context.
		/// </summary>
		/// <param name="controllerContext">The controller context.</param>
		/// <param name="viewName">The name of the view.</param>
		/// <param name="masterName">The name of the master (layout).</param>
		/// <param name="useCache"><see langword="true"/> to specify that the view engine returns the cached view, if a cached view exists; otherwise, <see langword="false"/>.</param>
		/// <returns>The page view.</returns>
		public virtual ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName, bool useCache)
		{
			if(controllerContext == null)
				throw new ArgumentNullException("controllerContext");

			if(string.IsNullOrEmpty(viewName))
				throw new ArgumentNullException("viewName");

			Logger.Trace(LoggerCategory.Api, () => string.Format("Entering FindView(): view={0}, master={1}, useCache={2}",
				viewName != null ? "\"" + viewName + "\"" : "(null)",
				masterName != null ? "\"" + masterName + "\"" : "(null)",
				useCache));

			IEnumerable<string> paths = PathsProvider.GetViewFolders(controllerContext);
			List<string> searchedPaths = new List<string>();

			var compiledView = GetCompiledView(controllerContext, viewName, paths, ref searchedPaths);

			if(compiledView != null)
			{
				List<CompiledView> layouts = new List<CompiledView>();

				string master = string.IsNullOrEmpty(masterName) ? compiledView.Layout : masterName;

				while(!string.IsNullOrEmpty(master))
				{
					//
					var compiledLayout = GetCompiledView(controllerContext, master, PathsProvider.GetLayoutFolders(controllerContext), ref searchedPaths);

					if(compiledLayout == null)
					{
						Logger.Trace(LoggerCategory.Api, () => "Leaving FindView(): view not found.");

						return ViewNotFound(searchedPaths);
					}

					if(layouts.Contains(compiledLayout))
					{
						throw new InvalidOperationException("Recursive layout found for \"" + master + "\"");
					}

					layouts.Add(compiledLayout);
					master = compiledLayout.Layout;
				}

				Logger.Trace(LoggerCategory.Api, () => "Leaving FindView(): returning found view.");

				return FoundView(CreateView(controllerContext, compiledView, layouts.ToArray()), this);
			}

			Logger.Trace(LoggerCategory.Api, () => "Leaving FindView(): view not found.");

			return ViewNotFound(searchedPaths);
		}

		/// <summary>
		/// Releases the specified view by using the specified controller context.
		/// </summary>
		/// <param name="controllerContext">The controller context.</param>
		/// <param name="view">The view.</param>
		public virtual void ReleaseView(ControllerContext controllerContext, IView view)
		{
			Logger.Trace(LoggerCategory.Api, () => "Entering ReleaseView()");

			IDisposable disposable = view as IDisposable;
			if(disposable != null)
			{
				disposable.Dispose();
			}

			Logger.Trace(LoggerCategory.Api, () => "Leaving ReleaseView()");
		}

		private CompiledView GetCompiledView(ControllerContext controllerContext, string viewName, IEnumerable<string> paths, ref List<string> searchedPaths)
		{
			CompiledView compiledView = null;

			// In case a specific path was requested, eg. "~/Views/Home/Index.hbs"
			if(viewName.StartsWith("~/"))
			{
				if(viewName.EndsWith(this.ViewExtension, StringComparison.OrdinalIgnoreCase))
				{
					searchedPaths.Add(viewName);

					compiledView = GetCompiledViewInternal(controllerContext, viewName);
				}
			}
			else
			{
				string filename = viewName + ViewExtension;

				foreach(var path in paths)
				{
					string virtualPath = Path.Combine(path, filename);

					searchedPaths.Add(virtualPath);

					compiledView = GetCompiledViewInternal(controllerContext, virtualPath);

					if(compiledView != null)
						break;
				}
			}

			return compiledView;
		}

		private CompiledView GetCompiledViewInternal(ControllerContext controllerContext, string virtualPath)
		{
			// virtualPath must be in the form "~/Views/Home/Index.hbs"
			if(virtualPath.StartsWith("~/"))
			{
				if(virtualPath.EndsWith(this.ViewExtension, StringComparison.OrdinalIgnoreCase))
				{
					DateTime utcstart = DateTime.UtcNow;
					string cachekey = GetCacheKey(controllerContext, virtualPath);
					object o = Cache.Get(cachekey);

					if(o != null && o is CompiledView)
					{
						var compiledView = o as CompiledView;

						// If FileHash is non-null we must check it still matches.
						// If it is null we used CacheDependency and this is not necessary.

						if(compiledView.FileHash != null)
						{
							string currentHash = VirtualPathProvider.GetFileHash(virtualPath, new string[] { virtualPath});

							if(compiledView.FileHash != currentHash)
							{
								// They don't match! Remove from cache and pretend it wasn't there to begin with.

								Cache.Remove(cachekey);
								o = null;
							}
						}
					}

					if(o == null)
					{
						if(FileExists(controllerContext, virtualPath))
						{
							Func<object,string> func;
							string              layout;

							if(CompileView(controllerContext, virtualPath, out func, out layout))
							{
								//
								var dependency = VirtualPathProvider.GetCacheDependency(virtualPath, virtualPathDependencies: new string[] { virtualPath }, utcStart: utcstart);

								// If the VPP supports CacheDependency, use a sliding expiration. If it doesn't, use absolute expiration instead.
								// Also, if the VPP doesn't support CacheDependency, get the filehash because we need to check that each time.
								// (If compiledView.FileHash is not null we need to check it next time. If it is null we know we used CacheDependency so no need to check the hash.)

								string fileHash = null;

								if(dependency == null)
								{
									fileHash = VirtualPathProvider.GetFileHash(virtualPath, new string[] { virtualPath });
								}

								CompiledView compiledView = new CompiledView(func, fileHash, layout);

								if(ViewsSlidingCacheTime != null)
								{
									var configcachekeyarray = new string[] { GetCacheKey(controllerContext, STR_CONFIG) };

									if(dependency != null)
									{
										dependency = new CacheDependency(null, configcachekeyarray, dependency);	// make it also dependent on the config cachekey

										Cache.Insert(cachekey, compiledView, dependency, Cache.NoAbsoluteExpiration, ViewsSlidingCacheTime);
									}
									else
									{
										dependency = new CacheDependency(null, configcachekeyarray); // make it dependent on the config cachekey

										Cache.Insert(cachekey, compiledView, dependency, utcstart.AddSeconds(30), Cache.NoSlidingExpiration);
									}
								}

								o = compiledView;
							}
							else
							{
								// There was some problem compiling it. Treat it as missing for a short period of time.
								o = FileDoesntExist.Instance;
								Cache.Insert(cachekey, o, null, utcstart.AddSeconds(2), Cache.NoSlidingExpiration);
							}
						}
						else
							o = FileDoesntExist.Instance;	// this has already been added to the cache (taken care of in FileExists())
					}

					if(o is CompiledView)
					{
						return o as CompiledView;
					}
				}
				return null;
			}
			return null;
		}

		private bool CompileView(ControllerContext controllerContext, string virtualPath, out Func<object, string> func, out string layout)
		{
			Func<object,string> compiledFunc = null;
			string              layoutInFile = null;
			bool success = false;

			var file = VirtualPathProvider.GetFile(virtualPath);

			using(var tr = new StreamReader(file.Open()))
			{
				var s = tr.ReadToEnd();

				var handlebars = GetHandlebars(controllerContext);

				Logger.Trace(LoggerCategory.Compile, () => "Compiling virtualpath '" + virtualPath + "'.");

				compiledFunc = handlebars.Compile(s);

				success = true;

				// Now get the layout if it was specified
				var layoutStart = "{{!<";
				if(s.StartsWith(layoutStart))
				{
					var i = s.IndexOf("}}");

					layoutInFile = s.Substring(layoutStart.Length, i - layoutStart.Length);
					layoutInFile = layoutInFile.Trim();

					// REVIEW: Should I handle parent paths ("../")? Or "blabla/blabla"? And dynamic paths (in parenthesis, meaning layout specified in a variable)?
					// Also, is it possible there are quotes around the string?

					if(Regex.IsMatch(layoutInFile, "^[a-zA-Z_][a-zA-Z0-9_]*$"))
					{
						Logger.Trace(LoggerCategory.Compile, () => string.Format("For virtualpath '{0}' the layout is '{1}'.", virtualPath, layoutInFile));
					}
					else
					{
						Logger.Warn(LoggerCategory.Compile, () => string.Format("For virtualpath '{0}' the layout was specified as '{1}' but that doesn't look like a valid identifier - ignoring it.", virtualPath, layoutInFile));

						layoutInFile = null;
					}
				}
				else {
					Logger.Trace(LoggerCategory.Compile, () => string.Format("Virtualpath '{0}' doesn't use a layout.", virtualPath));
				}
			}

			// Set the 'out' parameters
			func   = compiledFunc;
			layout = layoutInFile;

			return success;
		}

		private static object _handlebarslock = new object();

		private IHandlebars GetHandlebars(ControllerContext controllerContext)
		{
			string handlebarskey = GetCacheKey(controllerContext, STR_CONFIG);

			IHandlebars handlebars = Cache.Get(handlebarskey) as IHandlebars;

			if(handlebars == null)
			{
				lock(_handlebarslock)
				{
					handlebars = Cache.Get(handlebarskey) as IHandlebars;

					if(handlebars == null)
					{
						handlebars = GetHandlebarsLocked(controllerContext, handlebarskey);
					}
				}
			}

			return handlebars;
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The exception is logged.")]
		private IHandlebars GetHandlebarsLocked(ControllerContext controllerContext, string handlebarskey)
		{
			HandlebarsConfiguration config;
			IHandlebars handlebars = CreateHandlebars(out config);

			List<string> partialsfolders = PathsProvider.GetPartialsFolders(controllerContext).ToList();
			List<string> partialspaths = new List<string>();
			List<string> ignoredfiles = new List<string>();
			List<CacheDependency> partialsdependencies = new List<CacheDependency>();
			DateTime utcstart = DateTime.UtcNow;

			foreach(var virtualpath in PartialsScanner(partialsfolders))
			{
				string abspath   = VirtualPathUtility.ToAbsolute(virtualpath, HttpRuntime.AppDomainAppVirtualPath ?? "/");	// The last part is to make it work in unit tests
				string filename  = VirtualPathUtility.GetFileName(abspath);
				string extension = VirtualPathUtility.GetExtension(abspath);
				string basename  = filename.Substring(0, filename.Length - extension.Length);

				// Make it usable as a template name
				basename = basename.Replace(' ', '_').Replace('-', '_');

				if(config.RegisteredTemplates.ContainsKey(basename))
				{
					ignoredfiles.Add(virtualpath);
				}
				else
				{
					partialspaths.Add(virtualpath);

					using(var tr = new StreamReader(VirtualPathProvider.GetFile(abspath).Open()))
					{
						try
						{
							Logger.Trace(LoggerCategory.Compile, () => string.Format("Compiling partial '{0}'.", virtualpath));

							var partial = handlebars.Compile(tr);

							handlebars.RegisterTemplate(basename, partial);
						}
						catch(Exception ex)
						{
							Logger.Warn(LoggerCategory.Compile, () => string.Format("Failed to compile partial \"{0}\", ignoring it. Exception: {1}", virtualpath, ex.ToString()));

							// Without re-throwing, the user won't know there was a problem compiling the partial.
							// With re-throwing, it can make the entire website unusable until fixed (for example if it was for an admin controller).
							// It should probably be configurable what to do.

							//throw;
						}
					}

					// Add this dependency even if it didn't compile. If the user fixes the file (assuming syntax error) we need to rebuild the 'handlebars'.

					var dep = VirtualPathProvider.GetCacheDependency(virtualpath, new string[] { virtualpath }, utcstart);

					if(dep != null)
					{
						partialsdependencies.Add(dep);
					}
				}
			}

			// Add the folders to the dependencies too.

			foreach(var virtualfolder in partialsfolders)
			{
				CacheDependency dep = VirtualPathProvider.GetCacheDependency(virtualfolder, new string[] { virtualfolder }, utcstart);

				if(dep != null)
				{
					partialsdependencies.Add(dep);
				}
			}

			string[] globalcachekeyarray = new string[] { GetCacheKey(controllerContext, STR_GLOBAL) };

			var globcachedep = new CacheDependency(null, globalcachekeyarray);

			var dependencies = new AggregateCacheDependency();
			dependencies.Add(globcachedep);

			bool manualchecking = false;

			if(partialsdependencies.Count() > 0)
			{
				dependencies.Add(partialsdependencies.ToArray());
			}
			else
			{
				manualchecking = true;
			}

			Cache.Insert(handlebarskey, handlebars, dependencies);

			if(manualchecking)
			{
				// If the VirtualPathProvider doesn't support dependencies then the compiled partials needs to be checked for filehash changes.
				// What we do here is add a new cache item that has the files and paths to check for updates, that uses the CacheItemUpdateCallback delegate!
				// If any of the files changed, remove the handlebars key.
				// If not, reinstate this new cache item that uses CacheItemUpdateCallback.
				// It will be a little like a cron task.

				string[] handlebarskeyarray = new string[] { handlebarskey };

				var dep = new CacheDependency(null, handlebarskeyarray);

				var manualCheckData = new ManualCheckData();
				manualCheckData.PartialsFolders = partialsfolders;
				partialspaths.ForEach(path => manualCheckData.PartialsFilehashes.Add(path, VirtualPathProvider.GetFileHash(path, new string[] { path })));
				manualCheckData.IgnoredFiles = ignoredfiles;
				manualCheckData.ConfigCachekey = handlebarskey;

				Logger.Trace(LoggerCategory.Cache, () => string.Format("Adding manual dependency checking. folders:{0}, filehashes:{1}, ignored:{2}",
					manualCheckData.PartialsFolders.Count,
					manualCheckData.PartialsFilehashes.Count,
					manualCheckData.IgnoredFiles.Count));

				Cache.Insert(handlebarskey + Guid.NewGuid().ToString("N"), manualCheckData, dep, DateTime.Now.AddSeconds(5), Cache.NoSlidingExpiration, CheckPartialsCallback);
			}

			return handlebars;
		}

		private IHandlebars CreateHandlebars(out HandlebarsConfiguration config)
		{
			// Create a copy of the configuration so we can add templates according to the path without disturbing other invocations.
			// The FileSystem property is special because we need to figure out what cache dependencies to use.
			// But it isn't used??

			config = new HandlebarsConfiguration();

			foreach(var kvp in this.HandlebarsConfiguration.BlockHelpers)
			{
				config.BlockHelpers.Add(kvp.Key, kvp.Value);
			}
			foreach(var kvp in this.HandlebarsConfiguration.Helpers)
			{
				config.Helpers.Add(kvp.Key, kvp.Value);
			}
			foreach(var kvp in this.HandlebarsConfiguration.RegisteredTemplates)
			{
				config.RegisteredTemplates.Add(kvp.Key, kvp.Value);
			}

			config.ExpressionNameResolver = this.HandlebarsConfiguration.ExpressionNameResolver;
			config.TextEncoder            = this.HandlebarsConfiguration.TextEncoder;
			//config.FileSystem             = new VirtualFileSystem(HostingEnvironment.VirtualPathProvider);

			return Handlebars.Create(config);
		}

		// This callback is only used if the VPP doesn't support CacheDependency and the cached item (the handlebarskey+guid) is about to expire.
		private void CheckPartialsCallback(string key, CacheItemUpdateReason reason, out object expensiveObject, out CacheDependency dependency, out DateTime absoluteExpiration, out TimeSpan slidingExpiration)
		{
			// https://ngocthanhit.wordpress.com/2009/04/11/aspnet-cache-can-notify-you-before-an-entry-is-removed/

			// There are two possible reasons we can be called:
			// A dependency changed.
			//		Because the only thing we depend on is the cached handlebars configuration,
			//		this case means it was removed. Just remove ourself if so.
			// The absolute or sliding expiration interval expired.
			//		In this case we should check that partials haven't been added to or
			//		removed from the folders, and that the partials haven't changed.
			//		If no change: reinstate ourself again. Else, remove us _and_ the handlebars config from the cache.

			Logger.Trace(LoggerCategory.Cache, () => string.Format("CheckPartialsCallback called with reason={0}", reason));

			var manualCheckData = Cache.Get(key) as ManualCheckData;

			if(reason == CacheItemUpdateReason.DependencyChanged)
			{
				expensiveObject = null;
				dependency = null;
				absoluteExpiration = DateTime.UtcNow;
				slidingExpiration = Cache.NoSlidingExpiration;

				if(manualCheckData != null)
					Cache.Remove(manualCheckData.ConfigCachekey);

				return;
			}

			Logger.Trace(LoggerCategory.Cache, () => "CheckPartialsCallback: Checking partials for updates.");

			if(manualCheckData != null)
			{
				Dictionary<string,string> partials = new Dictionary<string, string>();
				List<string> ignored = new List<string>();

				bool same = true;	// initial state - assume they are the same

				foreach(var virtualpath in PartialsScanner(manualCheckData.PartialsFolders))
				{
					//string filename  = VirtualPathUtility.GetFileName(virtualpath);
					//string extension = VirtualPathUtility.GetExtension(virtualpath);
					//string basename  = filename.Substring(0, filename.Length - extension.Length);

					if(manualCheckData.PartialsFilehashes.ContainsKey(virtualpath))
					{
						string filehash = VirtualPathProvider.GetFileHash(virtualpath, new string[] { virtualpath });

						partials.Add(virtualpath, filehash);

						if(manualCheckData.PartialsFilehashes[virtualpath] != filehash)
						{
							Logger.Trace(LoggerCategory.Cache, () => string.Format("CheckPartialsCallback: Filehash is different for {0}", virtualpath));

							same = false;
							break;
						}
					}
					else
					{
						ignored.Add(virtualpath);

						if(manualCheckData.IgnoredFiles.Contains(virtualpath) == false)
						{
							Logger.Trace(LoggerCategory.Cache, () => string.Format("CheckPartialsCallback: File {0} is no longer missing", virtualpath));

							same = false;
							break;
						}
					}
				}

				//

				if( same &&
					partials.Count == manualCheckData.PartialsFilehashes.Count &&
					ignored.Count == manualCheckData.IgnoredFiles.Count )
				{
					// They are the same: reinstate ourself.

					Logger.Trace(LoggerCategory.Cache, () => "CheckPartialsCallback: Re-instating");

					string[] handlebarskeyarray = new string[] { manualCheckData.ConfigCachekey };

					var dep = new CacheDependency(null, handlebarskeyarray);

					expensiveObject = manualCheckData;
					dependency = dep;
					absoluteExpiration = DateTime.Now.AddSeconds(5);
					slidingExpiration = Cache.NoSlidingExpiration;
					return;
				}
			}

			// If we're still here the files differ in some way. Remove the cached config and remove ourself.

			Logger.Trace(LoggerCategory.Cache, () => "CheckPartialsCallback: Removing ourself.");

			expensiveObject = null;	// setting this to null will remove ourself
			dependency = null;
			absoluteExpiration = DateTime.UtcNow.AddSeconds(5);
			slidingExpiration = Cache.NoSlidingExpiration;

			Cache.Remove(manualCheckData.ConfigCachekey);
		}

		private IEnumerable<string> PartialsScanner(List<string> paths)
		{
			foreach(var path in paths)
			{
				var virdir = VirtualPathProvider.GetDirectory(path);

				if(virdir != null)
				{
					foreach(var virfile in virdir.Files.Cast<VirtualFile>())
					{
						string virtualpath = virfile.VirtualPath;
						string ext         = VirtualPathUtility.GetExtension(virtualpath);

						if(string.Equals(ext, this.ViewExtension, StringComparison.OrdinalIgnoreCase))
						{
							yield return VirtualPathUtility.ToAppRelative(virtualpath, HttpRuntime.AppDomainAppVirtualPath ?? "/");	// The last part is to make it work in unit tests
						}
					}
				}
			}
		}

		//-----------------------------------------------------------------------------------------
		// These next ones are for convenience - just making them easily accessible for the client.
		//-----------------------------------------------------------------------------------------

		// From https://github.com/rexm/Handlebars.Net/blob/master/source/Handlebars/Handlebars.cs
		// public delegate void HandlebarsHelper(TextWriter output, dynamic context, params object[] arguments);
		// public delegate void HandlebarsBlockHelper(TextWriter output, HelperOptions options, dynamic context, params object[] arguments);

		/// <summary>
		/// Registers a template in this view engine's global configuration.
		/// </summary>
		/// <param name="templateName">The name of the template.</param>
		/// <param name="templateBody">The template action body.</param>
		public virtual void RegisterTemplate(string templateName, Action<TextWriter, object> templateBody)
		{
			HandlebarsConfiguration.RegisteredTemplates.AddOrUpdate(templateName, templateBody);
		}

		//public virtual void RegisterTemplate(string templateName, string template)
		//{
		//	using(var reader = new StringReader(template))
		//	{
		//		RegisterTemplate(templateName, Compile(reader));
		//	}
		//}

		/// <summary>
		/// Registers a helper in this view engine's global configuration.
		/// </summary>
		/// <param name="helperName">The name of the helper.</param>
		/// <param name="helperFunction">The function body for this helper.</param>
		/// <remarks>
		/// If you call this after the view engine has been registered, you need to call the <see cref="HandlebarsViewEngine.ClearCache"/> method.
		/// </remarks>
		/// <example>
		/// <code language="C#">
		/// var hbsve = new HandlebarsViewEngine();
		/// hbsve.RegisterHelper("link_to", (writer, context, parameters) => {
		///   writer.WriteSafeString("&lt;a href='" + context.url + "'&gt;" + context.text + "&lt;/a&gt;");
		/// });
		/// </code>
		/// <code title="controller" language="C#">
		/// public ActionResult Index()
		/// {
		///     var data = new {
		///         url = "https://github.com/rexm/handlebars.net",
		///         text = "Handlebars.Net"
		///     };
		///	    return View(data);
		/// }
		/// </code>
		/// <code title="view.hbs">
		/// Click here: {{link_to}}
		/// </code>
		/// <code title="Renders">
		/// <![CDATA[
		/// Click here: <a href='https://github.com/rexm/handlebars.net'>Handlebars.Net</a>
		/// ]]>
		/// </code>
		/// </example>
		[CLSCompliant(false)]
		public virtual void RegisterHelper(string helperName, HandlebarsHelper helperFunction)
		{
			HandlebarsConfiguration.Helpers.AddOrUpdate(helperName, helperFunction);
		}

		/// <summary>
		/// Registers a block helper in this view engine's global configuration.
		/// </summary>
		/// <param name="helperName">The name of the block helper.</param>
		/// <param name="helperFunction">The function body for this helper.</param>
		/// <remarks>
		/// If you call this after the view engine has been registered, you need to call the <see cref="HandlebarsViewEngine.ClearCache"/> method.
		/// </remarks>
		[CLSCompliant(false)]
		public virtual void RegisterHelper(string helperName, HandlebarsBlockHelper helperFunction)
		{
			HandlebarsConfiguration.BlockHelpers.AddOrUpdate(helperName, helperFunction);
		}

		//------------------------------------------------------------------------------------------
		// These three ExtractXxxxName static methods are only to make the values easily accessible.
		// The most likely users are IPathProvider implementations.
		//------------------------------------------------------------------------------------------

		/// <summary>
		/// Gets the area name from the controller context.
		/// </summary>
		/// <param name="controllerContext">The controller context.</param>
		/// <returns>The area name, or <see langword="null"/> if not within an area.</returns>
		public static string ExtractAreaName(ControllerContext controllerContext)
		{
			// This is what https://github.com/ASP-NET-MVC/aspnetwebstack/blob/master/src/System.Web.Mvc/AreaHelpers.cs does.

			if(controllerContext == null)
				throw new ArgumentNullException("controllerContext");

			RouteData routeData = controllerContext.RouteData;

			object area;
			if(routeData.DataTokens.TryGetValue("area", out area))
			{
				return area as string;
			}

			IRouteWithArea routeWithArea = routeData.Route as IRouteWithArea;
			if(routeWithArea != null)
			{
				return routeWithArea.Area;
			}

			Route castRoute = routeData.Route as Route;
			if(castRoute != null && castRoute.DataTokens != null)
			{
				return castRoute.DataTokens["area"] as string;
			}

			return null;
		}

		/// <summary>
		/// Gets the controller name from the controller context.
		/// </summary>
		/// <param name="controllerContext">The controller context.</param>
		/// <returns>The controller name.</returns>
		public static string ExtractControllerName(ControllerContext controllerContext)
		{
			if(controllerContext == null)
				throw new ArgumentNullException("controllerContext");

			return controllerContext.RouteData.GetRequiredString("controller");
		}

		/// <summary>
		/// Gets the action name from the controller context.
		/// </summary>
		/// <param name="controllerContext">The controller context.</param>
		/// <returns>The action name.</returns>
		public static string ExtractActionName(ControllerContext controllerContext)
		{
			if(controllerContext == null)
				throw new ArgumentNullException("controllerContext");

			return controllerContext.RouteData.GetRequiredString("action");
		}

		//------------------------------------------------------------------------------------------------------------------
		// Static helper functions.
		// Different constructors have different meanings. These helper functions are used so it reads better in the source.
		//------------------------------------------------------------------------------------------------------------------

		private static ViewEngineResult FoundView(IView view, IViewEngine viewEngine)
		{
			return new ViewEngineResult(view, viewEngine);
		}

		private static ViewEngineResult ViewNotFound(IEnumerable<string> searchedLocations)
		{
			return new ViewEngineResult(searchedLocations);
		}

		//------------------------------------------------------------------------------------------------------------------
		// Handlebars helper registration functions.
		//------------------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Registers the builtin helpers that act a lot like their Razor/WebForms counterpart site-wide in the <see cref="HandlebarsViewEngine.HandlebarsConfiguration"/>.
		/// </summary>
		/// <remarks>
		/// These are the ones registered if this is called.
		/// <para />
		/// 
		/// From the <c>Url</c> namespace
		/// <list type="bullet">
		/// <item>
		///		<term><see cref="Helpers.UrlAction">url_action</see></term>
		///		<description>Generates a fully qualified URL to an action method.</description>
		/// </item>
		/// <item>
		///		<term><see cref="Helpers.UrlContent">url_content</see></term>
		///		<description>Converts a virtual (relative) path to an application absolute path and renders the result.</description>
		/// </item>
		/// <item>
		///		<term><see cref="Helpers.UrlEncode">url_encode</see></term>
		///		<description>Encodes text so it is usable in a URL.</description>
		/// </item>
		/// </list>
		/// 
		/// From the <c>Html</c> namespace:
		/// <list type="bullet">
		/// <item>
		///		<term><see cref="Helpers.HtmlAntiForgeryToken">html_antiforgerytoken</see></term>
		///		<description>Generates a hidden form field (anti-forgery token) that is validated when the form is submitted.</description>
		/// </item>
		/// <item>
		///		<term><see cref="Helpers.HtmlRenderAction">html_renderaction</see></term>
		///		<description>Invokes the child action method using the specified parameters and renders the result inline in the parent view.</description>
		/// </item>
		/// </list>
		/// 
		/// The ones related to bundling:
		/// <list type="bullet">
		/// <item>
		///		<term><see cref="Helpers.StylesRender">styles_render</see></term>
		///		<description>Renders a link-tag to a stylesheet bundle</description>
		/// </item>
		/// <item>
		///		<term><see cref="Helpers.ScriptsRender">scripts_render</see></term>
		///		<description>Renders a script-tag to a javascript bundle</description>
		/// </item>
		/// </list>
		/// </remarks>
		public virtual void RegisterMvcHelpers()
		{
			RegisterHelper("url_action",            HandlebarsDotNet.Mvc.Helpers.UrlAction);
			RegisterHelper("url_content",           HandlebarsDotNet.Mvc.Helpers.UrlContent);
			RegisterHelper("url_encode",            HandlebarsDotNet.Mvc.Helpers.UrlEncode);
			RegisterHelper("styles_render",         HandlebarsDotNet.Mvc.Helpers.StylesRender);
			RegisterHelper("scripts_render",        HandlebarsDotNet.Mvc.Helpers.ScriptsRender);
			RegisterHelper("html_renderaction",     HandlebarsDotNet.Mvc.Helpers.HtmlRenderAction);
			RegisterHelper("html_antiforgerytoken", HandlebarsDotNet.Mvc.Helpers.HtmlAntiForgeryToken);
		}

		/// <summary>
		/// Registers the builtin helpers that act a lot like their Razor counterpart site-wide in the <see cref="HandlebarsViewEngine.HandlebarsConfiguration"/>.
		/// </summary>
		/// <remarks>
		/// These are the ones registered if this is called:
		/// <para />
		/// 
		/// <list type="bullet">
		/// <item>
		///		<term><see cref="Helpers.DefineSection">definesection</see></term>
		///		<description>Assigns content to a named section.</description>
		/// </item>
		/// <item>
		///		<term><see cref="Helpers.IsSectionDefined">issectiondefined</see></term>
		///		<description>Returns a value to be used within an #if subexpression whether a section name has been defined.</description>
		/// </item>
		/// <item>
		///		<term><see cref="Helpers.RenderSection">rendersection</see></term>
		///		<description>Renders the named section.</description>
		/// </item>
		/// </list>
		/// </remarks>
		public virtual void RegisterSectionsHelpers()
		{
			RegisterHelper("definesection",    HandlebarsDotNet.Mvc.Helpers.DefineSection);
			RegisterHelper("issectiondefined", HandlebarsDotNet.Mvc.Helpers.IsSectionDefined);
			RegisterHelper("rendersection",    HandlebarsDotNet.Mvc.Helpers.RenderSection);
		}
	}

	// This class is used when we are manually checking for file updates. Only used if the VirtualPathProvider doesn't support GetCacheDependency.
	internal class ManualCheckData
	{
		internal List<string> PartialsFolders;

		// The key is the virtual path, the value is the file hash
		internal Dictionary<string, string> PartialsFilehashes = new Dictionary<string,string>();

		// The virtualpaths to ignored files (already registered in HandlebarsConfiguration)
		internal List<string> IgnoredFiles;

		internal string ConfigCachekey;		// This is the cachekey to remove if there were any changes for the partials (the cachekey for Handlebars config)
	}
}
