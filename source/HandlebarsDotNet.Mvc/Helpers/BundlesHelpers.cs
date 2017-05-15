using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Optimization;

// This file has the helpers related to rendering bundles (in Razor @Scripts.Render and @Styles.Render)
// Note: namespace doesn't end in ".Helpers" despite being in a Helpers folder.

// Some general guidance about bundling: https://docs.microsoft.com/en-us/aspnet/mvc/overview/performance/bundling-and-minification

namespace HandlebarsDotNet.Mvc
{
	public static partial class Helpers
	{
		// Example: {{scripts_render "~/bundles/jquery"}}
		/// <summary>
		/// Renders script tags for the specified paths.
		/// </summary>
		/// <param name="writer">The TextWriter provided by HandlebarsDotNet</param>
		/// <param name="context">The context (model) provided by HandlebarsDotNet</param>
		/// <param name="arguments">The arguments from the view, provided by HandlebarsDotNet</param>
		/// <remarks>
		/// This helper is among the ones registered if <see cref="HandlebarsViewEngine.RegisterMvcHelpers"/> is called. If so, it is registered as <c>scripts_render</c> but you can choose your own name for this:
		/// <code language="C#">
		/// var hbsve = new HandlebarsViewEngine();
		/// hbsve.RegisterHelper("scripts_render", HandlebarsDotNet.Mvc.Helpers.ScriptsRender);
		/// </code>
		/// It works like <see href="https://msdn.microsoft.com/en-us/library/system.web.optimization.scripts.render.aspx"/>.
		/// <para/>
		/// <h1><b>Usage</b></h1>
		/// <c>{{scripts_render [path ...]}}</c>
		/// <h2>Arguments</h2>
		/// string [path] (optional) - A set of virtual paths for which to generate script tags.
		/// <h2>Description</h2>
		/// This helper generates multiple script tags for each item in the bundle if <see href="https://msdn.microsoft.com/en-us/library/system.web.optimization.bundletable.enableoptimizations.aspx">EnableOptimizations</see> is set to <see langword="false"/>.
		/// When optimizations are enabled, it generates a single script tag to a version-stamped URL which represents the entire bundle.<br />
		/// See <see href="https://docs.microsoft.com/en-us/aspnet/mvc/overview/performance/bundling-and-minification"/> for some general guidance about bundling.
		/// <para/>
		/// The output is rendered with HandlebarsDotNet's WriteSafeString() meaning that using the triple-mustache is not needed.
		/// </remarks>
		/// <example>
		/// This example assumes this helper has been registered as "scripts_render" and optimizations are enabled.
		/// <code title="view.hbs">
		/// {{scripts_render "~/bundles/jquery"}}
		/// </code>
		/// <code title="Renders">
		/// &lt;script src="/bundles/jquery?v=JzhfglzUfmVF2qo-weTo-kvXJ9AJvIRBLmu11PgpbVY1"&gt;&lt;/script&gt;
		/// </code>
		/// </example>
		[SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "ScriptsRender")]
		[SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "context", Justification = "This is part of the signature for HandlebarsDotNet helpers.")]
		public static void ScriptsRender(TextWriter writer, dynamic context, params object[] arguments)
		{
			// Render a <script>-tag for a bundle (or several)

			if(writer == null)
				throw new ArgumentNullException("writer");

			if(arguments == null)
				throw new ArgumentNullException("arguments");

			// Each argument is supposed to be a string with the bundle name.
			// Hash arguments are not supported in this version, but it can be added at a later date to specify a script tag format, for example.

			var bundles = new List<string>();

			for(var i=0; i<arguments.Length; i++)
			{
				string bundlename = arguments[i] as string;		// "~/bundles/jquery"

				if(bundlename == null)
					throw new HelperException("ScriptsRender: Arguments are supposed to be strings with the path to a bundle, but one of them was "  + arguments[i].GetType());

				if(bundlename.Length > 0)
				{
					bundles.Add(bundlename);
				}
			}

			if(bundles.Count > 0)
			{
				var html = Scripts.Render(bundles.ToArray());

				writer.WriteSafeString(html);
			}
		}

		// Example: {{styles_render "~/content/css"}}
		/// <summary>
		/// Renders link tags for a set of paths.
		/// </summary>
		/// <param name="writer">The TextWriter provided by HandlebarsDotNet</param>
		/// <param name="context">The context (model) provided by HandlebarsDotNet</param>
		/// <param name="arguments">The arguments from the view, provided by HandlebarsDotNet</param>
		/// <remarks>
		/// This helper is among the ones registered if <see cref="HandlebarsViewEngine.RegisterMvcHelpers"/> is called. If so, it is registered as <c>styles_render</c> but you can choose your own name for this:
		/// <code language="C#">
		/// var hbsve = new HandlebarsViewEngine();
		/// hbsve.RegisterHelper("styles_render", HandlebarsDotNet.Mvc.Helpers.StylesRender);
		/// </code>
		/// It works like <see href="https://msdn.microsoft.com/en-us/library/system.web.optimization.styles.render.aspx"/>.
		/// <para/>
		/// <h1><b>Usage</b></h1>
		/// <c>{{styles_render [path ...]}}</c>
		/// <h2>Arguments</h2>
		/// string [path] (optional) - A set of virtual paths for which to generate link tags.
		/// <h2>Description</h2>
		/// This helper generates multiple link tags for each item in the bundle if <see href="https://msdn.microsoft.com/en-us/library/system.web.optimization.bundletable.enableoptimizations.aspx">EnableOptimizations</see> is set to <see langword="false"/>.
		/// When optimizations are enabled, it generates a single link tag to a version-stamped URL which represents the entire bundle.<br />
		/// See <see href="https://docs.microsoft.com/en-us/aspnet/mvc/overview/performance/bundling-and-minification"/> for some general guidance about bundling.
		/// <para/>
		/// The output is rendered with HandlebarsDotNet's WriteSafeString() meaning that using the triple-mustache is not needed.
		/// </remarks>
		/// <example>
		/// This example assumes this helper has been registered as "styles_render" and optimizations are enabled.
		/// <code title="view.hbs">
		/// {{styles_render "~/Content/css"}}
		/// </code>
		/// <code title="Renders">
		/// &lt;link href="/Content/css?v=WMr-pvK-ldSbNXHT-cT0d9QF2pqi7sqz_4MtKl04wlw1" rel="stylesheet"/&gt;
		/// </code>
		/// </example>
		[SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "StylesRender")]
		[SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "context", Justification = "This is part of the signature for HandlebarsDotNet helpers.")]
		public static void StylesRender(TextWriter writer, dynamic context, params object[] arguments)
		{
			// Render a <link>-tag for a style bundle (or several)

			if(writer == null)
				throw new ArgumentNullException("writer");

			if(arguments == null)
				throw new ArgumentNullException("arguments");

			// Each argument is supposed to be a string with the bundle name.
			// Hash arguments are not supported in this version, but it can be added at a later date to specify a script tag format, for example.

			var bundles = new List<string>();

			for(var i=0; i < arguments.Length; i++)
			{
				string bundlename = arguments[i] as string;		// "~/content/css"

				if(bundlename == null)
					throw new HelperException("StylesRender: Arguments are supposed to be strings with the path to a bundle, but one of them was " + arguments[i].GetType());

				if(bundlename.Length > 0)
				{
					bundles.Add(bundlename);
				}
			}

			if(bundles.Count > 0)
			{
				var html = Styles.Render(bundles.ToArray());

				writer.WriteSafeString(html);
			}
		}
	}
}
