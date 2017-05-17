using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace HandlebarsDotNet.Mvc
{
	/// <summary>
	/// Represents the information that is needed to render a Handlebars view in ASP.NET MVC.
	/// </summary>
	public class HandlebarsView : IView, IViewDataContainer
	{
		//private ControllerContext _controllerContext;
		private CompiledView      _view;
		private CompiledView[]    _layouts;

		/// <summary>
		/// Initializes a new instance of the HandlebarsView class using the controller context and compiled view and layouts.
		/// </summary>
		/// <param name="controllerContext">The controller context.</param>
		/// <param name="view">The compiled view.</param>
		/// <param name="layouts">The compiled layout(s) that will surround the output from the view. This can be <see langword="null" />.</param>
		public HandlebarsView(ControllerContext controllerContext, CompiledView view, CompiledView[] layouts)
		{
			if(controllerContext == null)
				throw new ArgumentNullException("controllerContext");

			if(view == null)
				throw new ArgumentNullException("view");

			// 'layouts' can be null. It can also be an array with no entries.

			//_controllerContext = controllerContext;
			_view              = view;
			_layouts           = layouts;
		}

		/// <summary>
		/// Renders the specified view context by using the specified writer object.
		/// </summary>
		/// <param name="viewContext">Information related to rendering a view, such as view data, temporary data, and form context.</param>
		/// <param name="writer">The writer object.</param>
		public virtual void Render(ViewContext viewContext, TextWriter writer)
		{
			if(viewContext == null)
				throw new ArgumentNullException("viewContext");

			if(writer == null)
				throw new ArgumentNullException("writer");

			ViewData = viewContext.ViewData;

			var context = SetupContext(viewContext);

			string html = _view.Func(context);

			if(_layouts != null && _layouts.Length > 0)
			{
				foreach(var layout in _layouts)
				{
					context["body"] = html;
					html = layout.Func(context);
				}

				// 'html' gets bigger and bigger for each loop, potentially making it a Gen2 object.
				// As nested layouts are probably not that common I won't fix this at the moment (if there is a fix).
				//
				// Just brainstorming a little: Instead of rendering the view first then each layout in order with 'body' as the result from the previous step,
				// do it in reverse: render the outermost layout and when the {{{body}}} is hit render the next (previous) layout and so on. There is probably
				// some opportunity to use a Handlebars helper registered as 'body' for this. (That isn't registered when the view executes...)
				// This would make bundle-handling (the view defines what bundles to render in the layout) harder, a use-case I envision will be quite common.
				// If disregarding the bundle-handling, it would however allow us to not keep temporary strings but allow us to write directly to the TextWriter.
				//
				// As it is at the moment (view first then layouts) makes it possible to have a Handlebars helper that allows setting a viewbag value from a view file
				// which is rendered in the layout. This is quite common in the example website for a Razor MVC application (the ViewBag.Title).
				// Another example is the section helpers.
			}

			writer.Write(html);
		}

		// Creates the context (Handlebars lingo for the model) and stores references to the ViewData and ViewContext.
		// Makes it a dictionary so we can add our custom items, and for speedy retrieval of model data.
		/// <summary>
		/// Creates the context (Handlebars lingo for the model) and stores references to the ViewData and ViewContext.
		/// </summary>
		/// <param name="viewContext">The ViewContext that contains the model.</param>
		/// <returns>A newly created context that will be used as the model for views and layouts in HandlebarsDotNet.</returns>
		/// <remarks>
		/// This method constructs a context object from <c>viewContext.ViewData.Model</c>.<br />
		/// The context will have a few extra properties added and those are:<br />
		/// <c>"viewbag"</c> - so a view file can use <c>{{viewbag.title}}</c> for example to output a <c>ViewBag.Title</c> property that was set in the controller,<br />
		/// and the other one is named in an obscure way (to minimize name clash with user properties) and contains the <see cref="ViewContext"/> that was passed to this method.
		/// <para/>
		/// Helpers may need to get ahold of the ViewContext and in those cases they can use the <see cref="GetViewContext">HandlebarsView.GetViewContext()</see> static method.
		/// </remarks>
		[SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification="Making it static would make it non-overrideable")]
		protected virtual dynamic SetupContext(ViewContext viewContext)
		{
			if(viewContext == null)
				throw new ArgumentNullException("viewContext");

			IDictionary<string,object> context;

			if(viewContext.ViewData.Model != null)
				context = CreateDictionary(viewContext.ViewData.Model);
			else
				context = new Dictionary<string, object>();

			context["viewbag"] = viewContext.ViewData;		// This makes the other stuff in ViewData available to the view as "viewbag"-variables (eg. viewbag.title). Maybe should be as a "@"-variable (@viewbag.title)? But how?

			context[Context_ViewContext] = viewContext;		// Store this in case it is needed (eg from a helper). Can be retrieved with HandlebarsView.GetViewContext()

			return context;
		}

		private static readonly string Context_ViewContext = "@.hbsVE:ViewContext";

		// TODO: Fix this example when the Handlebars.Net nuget package is updated
		/// <summary>
		/// Gets the <see cref="ViewContext"/> that was stored in the context when rendering began for the view.
		/// </summary>
		/// <param name="context">The context as returned by <see cref="SetupContext"/>.</param>
		/// <returns>The <see cref="ViewContext"/> for this view.</returns>
		/// <example>
		/// This example registers a helper globally for the view engine. In this case it needs the ViewContext in order to instantiate a HtmlHelper.<br />
		/// Note that this is only an example and doesn't include error checking.
		/// <code language="C#" title="Global.asax.cs">
		/// <![CDATA[
		/// var hbsve = new HandlebarsViewEngine();
		/// hbsve.RegisterHelper("formatvalue", (writer, context, args) =>
		/// {
		/// 	object val    = args[0];
		/// 	string format = args[1] as string;
		/// 	ViewContext viewContext = HandlebarsView.GetViewContext(context);
		/// 	HtmlHelper htmlHelper = new HtmlHelper(viewContext, viewContext.View as HandlebarsView);
		/// 	string formatted = htmlHelper.FormatValue(val, format);
		/// 	writer.Write(formatted);
		/// });
		/// ViewEngines.Engines.Add(hbsve);
		/// ]]>
		/// </code>
		/// <code language="C#" title="controller">
		/// public ActionResult Index()
		/// {
		///		var model = new { pi = 3.14159265358979 };
		///		return View(model);
		/// }
		/// </code>
		/// <code title="index.hbs">
		/// {{formatvalue pi "Pi is about {0:N}"}}
		/// </code>
		/// <code title="Renders">
		/// Pi is about 3.14
		/// </code>
		/// </example>
		public static ViewContext GetViewContext(dynamic context)
		{
			if(context == null)
				throw new ArgumentNullException("context");

			return context[Context_ViewContext] as ViewContext;
		}

		/// <summary>
		/// Gets or sets the view data dictionary.
		/// </summary>
		[SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification="This is part of the IViewDataContainer interface.")]
		public ViewDataDictionary ViewData
		{
			get
			{
				if(_viewData == null)
					_viewData = new ViewDataDictionary();
				return _viewData;
			}
			set
			{
				_viewData = value;
			}
		}
		private ViewDataDictionary _viewData;

		private static IDictionary<string, object> CreateDictionary(object source)
		{
			// REVIEW: What kind of StringComparer should be used?
			var stringComparer = StringComparer.InvariantCultureIgnoreCase;

			if(source is IDictionary)
			{
				// Already a dictionary, but we're going to add properties so make a copy of it.
				// REVIEW: Rewrite this to support non-string based keys (for example index keys)? But how would a view file access those?
				var sourcedict = (IDictionary) source;
				var newdict = new Dictionary<string, object>(stringComparer);
				foreach(var k in sourcedict.Keys)
				{
					newdict[k.ToString()] = sourcedict[k];
				}
				return newdict;
			}

			BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Instance;

			var dictionary = source.GetType().GetProperties(bindingAttr).ToDictionary
			(
				propInfo => propInfo.Name,
				propInfo => propInfo.GetValue(source, null),
				stringComparer
			);

			foreach(var fieldInfo in source.GetType().GetFields(bindingAttr))
			{
				dictionary.Add(fieldInfo.Name, fieldInfo.GetValue(source));
			}

			return dictionary;
		}
	}
}
