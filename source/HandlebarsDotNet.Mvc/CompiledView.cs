using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandlebarsDotNet.Mvc
{
	/// <summary>
	/// This class is used by <see cref="HandlebarsView"/> when rendering.
	/// </summary>
	/// <remarks>
	/// This is what is stored in the application cache by <see cref="HandlebarsViewEngine"/>.
	/// When the view engine adds this to the cache it has knowledge of paths searched, the virtual path to the source file, etc. That info is used for the cachekey and creating the CacheDependency.
	/// <note>
	/// You are not supposed to add to the cache yourself, that is handled by this view engine.<br />
	/// However, if you need to call <see cref="HandlebarsView.Render">HandlebarsView.Render()</see> yourself you need to be able to create instances of this class which is why this class is <see langword="public"/>.
	/// </note>
	/// </remarks>
	public class CompiledView
	{
		/// <summary>
		/// The compiled function.
		/// </summary>
		public Func<object, string> Func { get; private set; }

		/// <summary>
		/// The hash as returned from the VirtualPathProvider. This is <see langword="null"/> if the VirtualPathProvider supports CacheDependency for the file.
		/// </summary>
		public string FileHash { get; private set; }

		/// <summary>
		/// The layout specified in the file. Null or the empty string if not specified in it. Not used when rendering.
		/// </summary>
		/// <remarks>
		/// When executing the function the layout isn't rendered. This makes it easy for this view engine to render the layout specified, a different layout or no layout at all (in MVC lingo a "partial view").
		/// </remarks>
		public string Layout { get; private set; }

		// It would be nice if I could get hold of this
		//public IReadOnlyList<string> PartialsUsed { get; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="func"></param>
		/// <param name="fileHash"></param>
		/// <param name="layout"></param>
		public CompiledView(Func<object,string> func, string fileHash, string layout)
		{
			this.Func     = func;
			this.FileHash = fileHash;
			this.Layout   = layout;
		}
	}
}
