using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Routing;

namespace HandlebarsDotNet.Mvc
{
	/// <summary>
	/// This class is used as default by <see cref="HandlebarsViewEngine"/> to get the folder paths to be used when looking for views and other resources.
	/// </summary>
	/// <remarks>
	/// <para />The default folder paths for views are:
	/// <list type="table">
	///		<listheader>
	///			<term>View Folder Path<br />(as returned by DefaultPathsProvider)</term>
	///			<term>Note</term>
	///			<term>Example view file path<br />(area: Users, controller: HomeController, action: Index)</term>
	///		</listheader>
	///		<item>
	///			<description>~/Areas/{area}/Views/{controller}/</description>
	///			<description>Only if within an area.</description>
	///			<description>~/Areas/Users/Views/Home/Index.hbs</description>
	///		</item>
	///		<item>
	///			<description>~/Areas/{area}/Views/_Shared/</description>
	///			<description>Only if within an area. Note that "_Shared" is configurable (can also be empty, and if so this is not returned).</description>
	///			<description>~/Areas/Users/Views/_Shared/Index.hbs</description>
	///		</item>
	///		<item>
	///			<description>~/Areas/{area}/Views/</description>
	///			<description>Only if within an area, and only if <see cref="AllowControllerlessViewFolder"/> is <see langword="true"/>.</description>
	///			<description>~/Areas/Users/Views/Index.hbs</description>
	///		</item>
	///		<item>
	///			<description>~/Views/{controller}/</description>
	///			<description></description>
	///			<description>~/Views/Home/Index.hbs</description>
	///		</item>
	///		<item>
	///			<description>~/Views/_Shared/</description>
	///			<description>Note that "_Shared" is configurable (can also be empty, and if so this is not returned).</description>
	///			<description>~/Views/_Shared/Index.hbs</description>
	///		</item>
	///		<item>
	///			<description>~/Views/</description>
	///			<description>Only if <see cref="AllowControllerlessViewFolder"/> is <see langword="true"/>.</description>
	///			<description>~/Views/Index.hbs</description>
	///		</item>
	/// </list>
	/// 
	/// <para />The default folder paths for layouts are:
	/// <list type="table">
	///		<listheader>
	///			<term>Layout Folder Path<br />(as returned by DefaultPathsProvider)</term>
	///			<term>Note</term>
	///			<term>Example layout file path<br />(area: Users, controller: HomeController, action: Index, layout: standard)</term>
	///		</listheader>
	///		<item>
	///			<description>~/Areas/{area}/Views/{controller}/_Layouts/</description>
	///			<description>Only if within an area.</description>
	///			<description>~/Areas/Users/Views/Home/_Layouts/standard.hbs</description>
	///		</item>
	///		<item>
	///			<description>~/Areas/{area}/Views/_Layouts/</description>
	///			<description>Only if within an area.</description>
	///			<description>~/Areas/Users/Views/_Layouts/standard.hbs</description>
	///		</item>
	///		<item>
	///			<description>~/Views/{controller}/_Layouts/</description>
	///			<description></description>
	///			<description>~/Views/Home/_Layouts/standard.hbs</description>
	///		</item>
	///		<item>
	///			<description>~/Views/_Layouts/</description>
	///			<description></description>
	///			<description>~/Views/_Layouts/standard.hbs</description>
	///		</item>
	/// </list>
	/// 
	/// <para />The default folder paths for partials are:
	/// <list type="table">
	///		<listheader>
	///			<term>Partials Folder Path<br />(as returned by DefaultPathsProvider)</term>
	///			<term>Note</term>
	///			<term>Example partials file path<br />(area: Users, controller: HomeController, action: Index, partial: search)</term>
	///		</listheader>
	///		<item>
	///			<description>~/Areas/{area}/Views/{controller}/_Partials/</description>
	///			<description>Only if within an area.</description>
	///			<description>~/Areas/Users/Views/Home/_Partials/search.hbs</description>
	///		</item>
	///		<item>
	///			<description>~/Areas/{area}/Views/_Partials/</description>
	///			<description>Only if within an area.</description>
	///			<description>~/Areas/Users/Views/_Partials/search.hbs</description>
	///		</item>
	///		<item>
	///			<description>~/Views/{controller}/_Partials/</description>
	///			<description></description>
	///			<description>~/Views/Home/_Partials/search.hbs</description>
	///		</item>
	///		<item>
	///			<description>~/Views/_Partials/</description>
	///			<description></description>
	///			<description>~/Views/_Partials/search.hbs</description>
	///		</item>
	/// </list>
	/// </remarks>
	public class DefaultPathsProvider : IPathsProvider
	{
		// This class is meant to provide sensible defaults, and be easy to customize for clients.
		// They can either create an instance, change a property and pass to the constructor for HandlebarsViewEngine, or they can subclass it and override some method(s).
		// Or they can implement IPathsProvider themselves and not use this one.

		//----------------------
		// First the properties.
		//----------------------

		/// <summary>
		/// The name of the folder where views are located. Default: "Views"
		/// </summary>
		public string ViewsFolderName
		{
			get { return _viewsfoldername; }
			set { _viewsfoldername = value; }
		}
		private string _viewsfoldername = "Views";

		/// <summary>
		/// The name of the folder where areas are located. Default: "Areas"
		/// </summary>
		public string AreasFolderName
		{
			get { return _areasfoldername; }
			set { _areasfoldername = value; }
		}
		private string _areasfoldername = "Areas";

		/// <summary>
		/// The name of the folder where partials are located. Default: "_Partials"
		/// </summary>
		public string PartialsFolderName
		{
			get { return _partialsfoldername; }
			set { _partialsfoldername = value; }
		}
		private string _partialsfoldername = "_Partials";

		/// <summary>
		/// The name of the folder where layouts are located. Default: "_Layouts"
		/// </summary>
		public string LayoutsFolderName
		{
			get { return _layoutsfoldername; }
			set { _layoutsfoldername = value; }
		}
		private string _layoutsfoldername = "_Layouts";

		/// <summary>
		/// The folder name for shared view files (not specific to a certain controller). Set this to <see langword="null"/> if you want to disable this functionality. Default: "_Shared"
		/// </summary>
		/// <remarks>
		/// The built-in view engines in ASP.NET MVC uses the name "Shared" (without the underscore) for this.
		/// </remarks>
		public string SharedViewsFolderName
		{
			get { return _sharedviewsfoldername; }
			set { _sharedviewsfoldername = value; }
		}
		private string _sharedviewsfoldername = "_Shared";

		/// <summary>
		/// Whether to allow view files to be located directly in the <see cref="ViewsFolderName">Views</see> folder. Default: <see langword="false"/>.
		/// </summary>
		/// <remarks>
		/// This is primarily intended to be compatible with other Handlebars setups.
		/// </remarks>
		[SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Controllerless")]
		public bool AllowControllerlessViewFolder
		{
			get { return _allowcontrollerlessviewfolder; }
			set { _allowcontrollerlessviewfolder = value; }
		}
		private bool _allowcontrollerlessviewfolder = false;

		//-----------------------------
		// Here's the interface methods
		//-----------------------------

		/// <inheritdoc />
		public virtual IEnumerable<string> GetViewFolders(ControllerContext controllerContext)
		{
			string area       = HandlebarsViewEngine.ExtractAreaName(controllerContext);
			string controller = HandlebarsViewEngine.ExtractControllerName(controllerContext);

			string viewsfoldername = ViewsFolderName;
			string sharedviewsfoldername = SharedViewsFolderName;
			bool allowcontrollerlessviewfolder = AllowControllerlessViewFolder;

			if(!string.IsNullOrEmpty(area))
			{
				string areasfoldername = AreasFolderName;

				yield return "~/" + areasfoldername + "/" + area + "/" + viewsfoldername + "/" + controller + "/";                  // "~/Areas/areaname/Views/controllername/"
				if(!string.IsNullOrEmpty(sharedviewsfoldername))
				{
					yield return "~/" + areasfoldername + "/" + area + "/" + viewsfoldername + "/" + sharedviewsfoldername + "/";   // "~/Areas/areaname/Views/_Shared/"
				}
				if(allowcontrollerlessviewfolder)
				{
					yield return "~/" + areasfoldername + "/" + area + "/" + viewsfoldername + "/";                                 // "~/Areas/areaname/Views/"
				}
			}
			yield return "~/" + viewsfoldername + "/" + controller + "/";                                                           // "~/Views/controllername/"
			if(!string.IsNullOrEmpty(sharedviewsfoldername))
			{
				yield return "~/" + viewsfoldername + "/" + sharedviewsfoldername + "/";                                            // "~/Views/_Shared/"
			}
			if(allowcontrollerlessviewfolder)
			{
				yield return "~/" + viewsfoldername + "/";                                                                          // "~/Views/"
			}
		}

		/// <inheritdoc />
		public virtual IEnumerable<string> GetLayoutFolders(ControllerContext controllerContext)
		{
			string area       = HandlebarsViewEngine.ExtractAreaName(controllerContext);
			string controller = HandlebarsViewEngine.ExtractControllerName(controllerContext);

			string viewsfoldername = ViewsFolderName;
			string sharedviewsfoldername = SharedViewsFolderName;
			string layoutsfoldername = LayoutsFolderName;

			if(!string.IsNullOrEmpty(area))
			{
				string areasfoldername = AreasFolderName;

				yield return "~/" + areasfoldername + "/" + area + "/" + viewsfoldername + "/" + controller + "/" + layoutsfoldername + "/";                // "~/Areas/areaname/Views/controllername/_Layouts/"

				yield return "~/" + areasfoldername + "/" + area + "/" + viewsfoldername + "/" + layoutsfoldername + "/";                                   // "~/Areas/areaname/Views/_Layouts/"
			}

			yield return "~/" + viewsfoldername + "/" + controller + "/" + layoutsfoldername + "/";                                                         // "~/Views/controllername/_Layouts/"

			yield return "~/" + viewsfoldername + "/" + layoutsfoldername + "/";                                                                            // "~/Views/_Layouts/"
		}

		/// <inheritdoc />
		public virtual IEnumerable<string> GetPartialsFolders(ControllerContext controllerContext)
		{
			string area       = HandlebarsViewEngine.ExtractAreaName(controllerContext);
			string controller = HandlebarsViewEngine.ExtractControllerName(controllerContext);

			string viewsfoldername = ViewsFolderName;
			string sharedviewsfoldername = SharedViewsFolderName;
			string partialsfoldername = PartialsFolderName;

			if(!string.IsNullOrEmpty(area))
			{
				string areasfoldername = AreasFolderName;

				yield return "~/" + areasfoldername + "/" + area + "/" + viewsfoldername + "/" + controller + "/" + partialsfoldername + "/";                   // "~/Areas/areaname/Views/controllername/_Partials/"

				yield return "~/" + areasfoldername + "/" + area + "/" + viewsfoldername + "/" + partialsfoldername + "/";                                      // "~/Areas/areaname/Views/_Partials/"
			}

			yield return "~/" + viewsfoldername + "/" + controller + "/" + partialsfoldername + "/";                                                            // "~/Views/controllername/_Partials/"

			yield return "~/" + viewsfoldername + "/" + partialsfoldername + "/";                                                                               // "~/Views/_Partials/"
		}
	}
}
