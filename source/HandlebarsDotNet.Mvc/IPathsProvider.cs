using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace HandlebarsDotNet.Mvc
{
	/// <summary>
	/// The interface used by <see cref="HandlebarsViewEngine"/> when getting the different paths.
	/// </summary>
	/// <remarks>
	/// Unless an implementation is provided when creating the <see cref="HandlebarsViewEngine"/>, the <see cref="DefaultPathsProvider"/> is used.
	/// </remarks>
	public interface IPathsProvider
	{
		/// <summary>
		/// Gets the folders for where the view files can be for the given ControllerContext.
		/// The paths returned are in the form <c>"~/Views/Home/"</c>.
		/// </summary>
		/// <param name="controllerContext">The ControllerContext for the current request.</param>
		/// <returns>Returns an ordered <see cref="IEnumerable{T}">IEnumerable&lt;string&gt;</see> with the paths for HandlebarsViewEngine to check.</returns>
		IEnumerable<string> GetViewFolders(ControllerContext controllerContext);

		/// <summary>
		/// Gets the folders for where the layout files can be for the given ControllerContext.
		/// The paths returned are in the form <c>"~/Views/_Layouts/"</c>.
		/// </summary>
		/// <param name="controllerContext">The ControllerContext for the current request.</param>
		/// <returns>Returns an ordered <see cref="IEnumerable{T}">IEnumerable&lt;string&gt;</see> with the paths for HandlebarsViewEngine to check.</returns>
		IEnumerable<string> GetLayoutFolders(ControllerContext controllerContext);

		/// <summary>
		/// Gets the folders for where the partials files can be for the given ControllerContext.
		/// The paths returned are in the form <c>"~/Views/_Partials/"</c>.
		/// </summary>
		/// <param name="controllerContext">The ControllerContext for the current request.</param>
		/// <returns>Returns an ordered <see cref="IEnumerable{T}">IEnumerable&lt;string&gt;</see> with the paths for HandlebarsViewEngine to check.</returns>
		IEnumerable<string> GetPartialsFolders(ControllerContext controllerContext);
	}
}
