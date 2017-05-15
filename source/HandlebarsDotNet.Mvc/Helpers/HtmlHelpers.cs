using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Routing;

// This file has the helpers that behaves like the Html helpers in ASP.NET MVC (Razor or Web Forms)
// Note: namespace doesn't end in ".Helpers" despite being in a Helpers folder.

namespace HandlebarsDotNet.Mvc
{
	public static partial class Helpers
	{
		// Example: {{html_antiforgerytoken}} = '<input type="hidden" name="__AntiForgeryToken" value="..." />'
		/// <summary>
		/// Generates a hidden form field (anti-forgery token) that is validated when the form is submitted.
		/// </summary>
		/// <param name="writer">The TextWriter provided by HandlebarsDotNet</param>
		/// <param name="context">The context (model) provided by HandlebarsDotNet</param>
		/// <param name="arguments">The arguments from the view, provided by HandlebarsDotNet</param>
		/// <remarks>
		/// This helper is among the ones registered if <see cref="HandlebarsViewEngine.RegisterMvcHelpers"/> is called. If so, it is registered as <c>html_antiforgerytoken</c> but you can choose your own name for this:
		/// <code language="C#">
		/// var hbsve = new HandlebarsViewEngine();
		/// hbsve.RegisterHelper("html_antiforgerytoken", HandlebarsDotNet.Mvc.Helpers.HtmlAntiForgeryToken);
		/// </code>
		/// It works like <see href="https://msdn.microsoft.com/en-us/library/system.web.mvc.htmlhelper.antiforgerytoken.aspx"/>.
		/// <para/>
		/// <h1><b>Usage</b></h1>
		/// <c>{{html_antiforgerytoken}}</c>
		/// <h2>Arguments</h2>
		/// (none)
		/// <h2>Description</h2>
		/// The anti-forgery token can be used to help protect your application against cross-site request forgery.
		/// To use this feature, render the anti-forgery token within a form and add the <see cref="ValidateAntiForgeryTokenAttribute"/> attribute to the action method that you want to protect.
		/// <para/>
		/// For information about cross-site request forgery (CSRF) see <see href="https://docs.microsoft.com/en-us/aspnet/mvc/overview/security/xsrfcsrf-prevention-in-aspnet-mvc-and-web-pages"/>.
		/// <para/>
		/// The output is rendered with HandlebarsDotNet's WriteSafeString() meaning that using the triple-mustache is not needed.
		/// </remarks>
		/// <example>
		/// This example assumes this helper has been registered as "html_antiforgerytoken".
		/// <code title="view.hbs">
		/// {{html_antiforgerytoken}}
		/// </code>
		/// <code title="Renders">
		/// &lt;input name="__RequestVerificationToken" type="hidden" value="UzCEoDwZ...(quite a long string)" /&gt;
		/// </code>
		/// </example>
		[SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "arguments", Justification = "This is part of the signature for HandlebarsDotNet helpers.")]
		[SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "context", Justification = "This is part of the signature for HandlebarsDotNet helpers.")]
		public static void HtmlAntiForgeryToken(TextWriter writer, dynamic context, params object[] arguments)
		{
			if(writer == null)
				throw new ArgumentNullException("writer");

			if(arguments == null)
				throw new ArgumentNullException("arguments");

			// This one exists in assembly System.Web.WebPages.dll
			var html = System.Web.Helpers.AntiForgery.GetHtml().ToString();

			writer.WriteSafeString(html);

			// REVIEW: Instead of having a hard reference to System.Web.WebPages.dll maybe do late-binding instead and output an error if the DLL isn't loaded.
		}

		// Example: {{html_renderaction "Index" "Home"}} => renders the action as a child request at that point in the source .hbs
		/// <summary>
		/// Invokes the child action method using the specified parameters and renders the result inline in the parent view.
		/// </summary>
		/// <param name="writer">The TextWriter provided by HandlebarsDotNet</param>
		/// <param name="context">The context (model) provided by HandlebarsDotNet</param>
		/// <param name="arguments">The arguments from the view, provided by HandlebarsDotNet</param>
		/// <remarks>
		/// This helper is among the ones registered if <see cref="HandlebarsViewEngine.RegisterMvcHelpers"/> is called. If so, it is registered as <c>html_renderaction</c> but you can choose your own name for this:
		/// <code language="C#">
		/// var hbsve = new HandlebarsViewEngine();
		/// hbsve.RegisterHelper("html_renderaction", HandlebarsDotNet.Mvc.Helpers.HtmlRenderAction);
		/// </code>
		/// It works like <see href="https://msdn.microsoft.com/en-us/library/system.web.mvc.html.childactionextensions.renderaction.aspx"/>.
		/// <para/>
		/// <h1><b>Usage</b></h1>
		/// <c>{{html_renderaction [action] [controller] [key=value ...]}}</c>
		/// <h2>Arguments</h2>
		/// <list type="definition">
		///	<item>
		///		<term>action</term>
		///		<description>string (required) - The name of the action.</description>
		///	</item>
		///	<item>
		///		<term>controller</term>
		///		<description>string (required) - The name of the controller (without "Controller").</description>
		///	</item>
		///	<item>
		///		<term>attributes</term>
		///		<description>(optional) - The rest of the attributes are used as route values.</description>
		///	</item>
		///	</list>
		/// <h2>Description</h2>
		/// <para/>
		/// The output is rendered with HandlebarsDotNet's WriteSafeString() meaning that using the triple-mustache is not needed.
		/// </remarks>
		/// <example>
		/// This example assumes this helper has been registered as "html_renderaction". Please note that the child action doesn't have to use the same view engine.
		/// <code title="view.hbs">
		/// Intro text&lt;br /&gt;
		/// {{html_renderaction "Details" "Widget" id="SupDup"}}
		/// </code>
		/// <code language="C#" title="WidgetController.cs">
		/// public WidgetController
		/// {
		///		public ActionResult Details(string id)
		///		{
		///			var model = new WidgetModel
		///			{
		///				Id = id,
		///				Description = "Super duper widget!"
		///			};
		///			return PartialView(model);
		///		}
		/// }
		/// </code>
		/// <code title="Details.cshtml"><![CDATA[
		///	@model WidgetModel
		///	<p>Id: @Model.Id</p>
		///	<p>@Model.Description</p>
		///	]]>
		///	</code>
		/// <code title="Renders"><![CDATA[
		/// Intro text<br />
		///	<p>Id: SupDup</p>
		///	<p>Super duper widget!</p>
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso href="https://msdn.microsoft.com/en-us/library/system.web.mvc.html.childactionextensions.renderaction.aspx"/>
		public static void HtmlRenderAction(TextWriter writer, dynamic context, params object[] arguments)
		{
			if(writer == null)
				throw new ArgumentNullException("writer");

			if(context == null)
				throw new ArgumentNullException("context");

			if(arguments == null)
				throw new ArgumentNullException("arguments");

			// We must be called with at least action and controller. Routevalues are optional.
			// REVIEW: Fix it so controller isn't required (just as the HtmlHelper)?

			if(arguments.Length == 2 || arguments.Length == 3)
			{
				string action     = arguments[0] as string;
				string controller = arguments[1] as string;

				if(string.IsNullOrEmpty(action))
					throw new HelperException("HtmlRenderAction: 'action' is required");

				if(string.IsNullOrEmpty(controller))
					throw new HelperException("HtmlRenderAction: 'controller' is required");

				var hash = arguments[arguments.Length - 1] as IDictionary<string, object>;

				RouteValueDictionary rvd = null;
				if(hash != null)
				{
					rvd = new RouteValueDictionary(hash);
				}

				ViewContext viewContext = HandlebarsView.GetViewContext(context);
				HtmlHelper htmlHelper = new HtmlHelper(viewContext, viewContext.View as HandlebarsView);

				var html = htmlHelper.Action(action, controller, rvd);

				writer.WriteSafeString(html);
			}
			else
				throw new HelperException("HtmlRenderAction: must be called with action and controller");
		}
	}
}
