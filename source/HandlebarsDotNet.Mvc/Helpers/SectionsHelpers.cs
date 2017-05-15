using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// This file has the helpers that work in a (very) similar fashion as sections in Razor: https://weblogs.asp.net/scottgu/asp-net-mvc-3-layouts-and-sections-with-razor
// Note: namespace doesn't end in ".Helpers" despite being in a Helpers folder.

// In express-hbs (https://github.com/barc/express-hbs) the helpers doing this are named "contentFor" and "block".
// A very different way to do this is given at https://github.com/shannonmoeller/handlebars-layouts

namespace HandlebarsDotNet.Mvc
{
	public static partial class Helpers
	{
		private static readonly string SectionsPrefix = "@.hbsVE:Sections_";	// by having at, dot and colon in this it makes it unlikely we have a conflict with user data in the model data

		// Block helper ("block" as in HandlebarsDotNet)
		// Example: {{#definesection "scripts"}}<script src="/Scripts/jquery-1.8.2.js"></script>{{/definesection}}
		/// <summary>
		/// Assigns content to a named section.
		/// </summary>
		/// <param name="writer">The TextWriter provided by HandlebarsDotNet</param>
		/// <param name="options">The HelperOptions provided by HandlebarsDotNet</param>
		/// <param name="context">The context (model) provided by HandlebarsDotNet</param>
		/// <param name="arguments">The arguments from the view, provided by HandlebarsDotNet</param>
		/// <remarks>
		/// This helper is among the ones registered if <see cref="HandlebarsViewEngine.RegisterSectionsHelpers"/> is called. If so, it is registered as <c>definesection</c> but you can choose your own name for this:
		/// <code language="C#">
		/// var hbsve = new HandlebarsViewEngine();
		/// hbsve.RegisterHelper("definesection", HandlebarsDotNet.Mvc.Helpers.DefineSection);
		/// </code>
		/// It works like Razor's <c>@section</c> statement. For a nice write-up, see <see href="https://weblogs.asp.net/scottgu/asp-net-mvc-3-layouts-and-sections-with-razor"/>.
		/// <para/>
		/// <h1><b>Usage</b></h1>
		/// <c>{{#definesection [name] [mode="replace"|"append"|"prepend"]}}<br />
		/// content here<br />
		/// {{/definesection}}</c>
		/// <h2>Arguments</h2>
		/// <list type="definition">
		///	<item>
		///		<term>name</term>
		///		<description>string (required) - The name of the section.</description>
		///	</item>
		///	<item>
		///		<term>mode</term>
		///		<description>string (required) - The operation mode. Specifying <c>mode</c> is not required but if specified it must be one of three values: <c>"replace"</c> (default), <c>"append"</c> or <c>"prepend"</c>.</description>
		///	</item>
		/// </list>
		/// <h2>Description</h2>
		/// This helper allows splitting up the normal contigous HTML contents in view files so a section can be rendered at a specified location within a layout.<br />
		/// Normally the whole contents from the view is rendered by <c>{{{body}}}</c> in a layout file, but this helper takes it out of the normal flow
		/// so it can be rendered at the specified point by <c>{{rendersection "..."}}</c> in the layout.<br />
		/// See the example for how it can be used.
		/// 
		/// <para/>The <c>mode</c> attribute can take different values:
		/// <list type="definition">
		///		<item>
		///			<term>mode="replace"</term>
		///			<description>This ignores any previously defined section with this name and just sets the contents. This is the default.</description>
		///		</item>
		///		<item>
		///			<term>mode="append"</term>
		///			<description>Appends the content to any previously defined section with this name.</description>
		///		</item>
		///		<item>
		///			<term>mode="prepend"</term>
		///			<description>Prepends the content to any previously defined section with this name.</description>
		///		</item>
		/// </list>
		/// For the <c>append</c> and <c>prepend</c> modes the named section doesn't have to be defined beforehand. In those cases it works as <c>mode="replace"</c>.
		/// </remarks>
		/// <example>
		/// <code title="view.hbs" language="none">
		/// <![CDATA[
		/// {{!< default}}
		/// {{#definesection "sidebar"}}
		///	    For more info, see<br />
		///	    <a href="link1.html">Link 1</a><br />
		///     <a href="link2.html">Link 2</a><br />
		/// {{/definesection}}
		/// ]]>
		/// </code>
		/// <code title="~/Views/_Layouts/default.hbs" language="none">
		/// <![CDATA[
		/// {{#if (issectiondefined "sidebar")}}
		///	    <div id="sidebar">
		///	        {{{rendersection "sidebar"}}}
		///	    </div>
		/// {{else}}
		///	    <p>Default sidebar content...</p>
		/// {{/if}}
		/// ]]>
		/// </code>
		/// </example>
		[SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "DefineSection")]
		[CLSCompliant(false)]
		public static void DefineSection(TextWriter writer, HelperOptions options, dynamic context, params object[] arguments)
		{
			// Assign content to a named section (typically used in the view)

			if(writer == null)
				throw new ArgumentNullException("writer");

			if(options == null)
				throw new ArgumentNullException("options");

			if(context == null)
				throw new ArgumentNullException("context");

			if(arguments == null)
				throw new ArgumentNullException("arguments");

			if(arguments.Length == 1 || arguments.Length == 2)
			{
				string name = arguments[0] as string;
				string mode = "replace";

				if(string.IsNullOrEmpty(name))
				{
					throw new HelperException("DefineSection: must be called with a section name.");
				}

				IDictionary<string,object> hash = null;

				if(arguments.Length == 2)
				{
					hash = arguments[1] as IDictionary<string, object>;

					if(hash == null)
					{
						throw new HelperException("DefineSection: Called with wrong arguments for section '" + name + "'.");
					}

					if(hash.Count != 1)
						throw new HelperException("DefineSection: Only one attribute is allowed and that is 'mode=replace|append|prepend'");

					if(hash.ContainsKey("mode"))
					{
						if(hash["mode"] is string)
						{
							mode = ((string) hash["mode"]).ToLowerInvariant();

							if(mode != "replace" && mode != "append" && mode != "prepend")
								throw new HelperException("DefineSection: Attribute 'mode' must have one of the values [replace,append,prepend] but was '" + hash["mode"] + "' (for section '" + name + "')");
						}
						else
							throw new HelperException("DefineSection: Attribute 'mode' must be a string with one of the values [replace,append,prepend] but was " + hash["mode"].GetType() + " '" + hash["mode"] + "' (for section '" + name + "')");
					}
				}

				string keyname = SectionsPrefix + name;

				if(!context.ContainsKey(keyname))
					context[keyname] = string.Empty;

				string html = string.Empty;
				using(var textWriter = new System.IO.StringWriter())
				{
					options.Template(textWriter, context);
					html = textWriter.GetStringBuilder().ToString();
				}

				if(mode == "replace")
				{
					// nothing to do here
				}
				else if(mode == "append")
				{
					html = context[keyname] + html;
				}
				else if(mode == "prepend")
				{
					html += context[keyname];
				}
				else
					throw new HelperException("DefineSection: Internal error - mode is '" + mode + "'");

				context[keyname] = html;
			}
			else
				throw new HelperException("DefineSection: must be called with a section name and possibly 'mode=replace|append|prepend'.");
		}

		// Example: {{#if (issectiondefined "sidebar")}}Yes!{{else}}No...{{/if}}
		/// <summary>
		/// Returns a value to be used within an #if subexpression whether a section name has been defined.
		/// </summary>
		/// <param name="writer">The TextWriter provided by HandlebarsDotNet</param>
		/// <param name="context">The context (model) provided by HandlebarsDotNet</param>
		/// <param name="arguments">The arguments from the view, provided by HandlebarsDotNet</param>
		/// <remarks>
		/// This helper is among the ones registered if <see cref="HandlebarsViewEngine.RegisterSectionsHelpers"/> is called. If so, it is registered as <c>issectiondefined</c> but you can choose your own name for this:
		/// <code language="C#">
		/// var hbsve = new HandlebarsViewEngine();
		/// hbsve.RegisterHelper("issectiondefined", HandlebarsDotNet.Mvc.Helpers.IsSectionDefined);
		/// </code>
		/// It works like Razor's IsSectionDefined method. For a nice write-up, see <see href="https://weblogs.asp.net/scottgu/asp-net-mvc-3-layouts-and-sections-with-razor"/>.
		/// <para/>
		/// <h1><b>Usage</b></h1>
		/// <c>{{#if (issectiondefined [name])}} ... {{/if}}</c>
		/// <h2>Arguments</h2>
		/// <list type="definition">
		///	<item>
		///		<term>name</term>
		///		<description>string (required) - The name of the section.</description>
		///	</item>
		/// </list>
		/// <h2>Description</h2>
		/// This helper checks if a named section has been defined, and returns a truthy value if so. That value can be used in an <c>{{#if}}</c>-test to for example render the section or default content as in the example below.
		/// </remarks>
		/// <example>
		/// <code title="view.hbs" language="none">
		/// <![CDATA[
		/// {{!< default}}
		/// {{#definesection "sidebar"}}
		///	    For more info, see<br />
		///	    <a href="link1.html">Link 1</a><br />
		///     <a href="link2.html">Link 2</a><br />
		/// {{/definesection}}
		/// ]]>
		/// </code>
		/// <code title="~/Views/_Layouts/default.hbs" language="none">
		/// <![CDATA[
		/// {{#if (issectiondefined "sidebar")}}
		///	    <div id="sidebar">
		///	        {{{rendersection "sidebar"}}}
		///	    </div>
		/// {{else}}
		///	    <p>Default sidebar content...</p>
		/// {{/if}}
		/// ]]>
		/// </code>
		/// </example>
		[SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "IsSectionDefined")]
		public static void IsSectionDefined(TextWriter writer, dynamic context, params object[] arguments)
		{
			// Returns a value to be used within an #if subexpression whether a section name has been defined.

			if(writer == null)
				throw new ArgumentNullException("writer");

			if(context == null)
				throw new ArgumentNullException("context");

			if(arguments == null)
				throw new ArgumentNullException("arguments");

			if(arguments.Length == 1)
			{
				string name = arguments[0] as string;

				if(string.IsNullOrEmpty(name))
					throw new HelperException("IsSectionDefined: must be called with a section name to check.");

				string keyname = SectionsPrefix + name;

				bool isdefined = context.ContainsKey(keyname);

				if(isdefined)
				{
					writer.Write(isdefined);
				}
				else
				{
					// Do not write anything in this case.
				}
			}
			else
				throw new HelperException("IsSectionDefined: must be called with a name of a section and no other arguments.");
		}

		// Regular helper
		// Example: {{{rendersection "scripts" required=false}}} = '<script src="/Scripts/jquery-1.8.2.js"></script>' (note the triple-mustache)
		/// <summary>
		/// Renders the named section.
		/// </summary>
		/// <param name="writer">The TextWriter provided by HandlebarsDotNet</param>
		/// <param name="context">The context (model) provided by HandlebarsDotNet</param>
		/// <param name="arguments">The arguments from the view, provided by HandlebarsDotNet</param>
		/// <remarks>
		/// This helper is among the ones registered if <see cref="HandlebarsViewEngine.RegisterSectionsHelpers"/> is called. If so, it is registered as <c>rendersection</c> but you can choose your own name for this:
		/// <code language="C#">
		/// var hbsve = new HandlebarsViewEngine();
		/// hbsve.RegisterHelper("rendersection", HandlebarsDotNet.Mvc.Helpers.RenderSection);
		/// </code>
		/// It works like Razor's RenderSection method. For a nice write-up, see <see href="https://weblogs.asp.net/scottgu/asp-net-mvc-3-layouts-and-sections-with-razor"/>.
		/// <para/>
		/// <h1><b>Usage</b></h1>
		/// <c>{{rendersection [name] [required=true|false])}}</c>
		/// <h2>Arguments</h2>
		/// <list type="definition">
		///	<item>
		///		<term>name</term>
		///		<description>string (required) - The name of the section.</description>
		///	</item>
		///	<item>
		///		<term>required</term>
		///		<description>bool (required) - Whether the section must have been defined. Specifying <c>required</c> is not required but if specified it must be <c>true</c> (default) or <c>false</c>.</description>
		///	</item>
		/// </list>
		/// <h2>Description</h2>
		/// Renders a named section.<br />
		/// If the section hasn't been defined and <c>required</c> is <see langword="true"/> (which it is by default) an exception is thrown.<br />
		/// If the section hasn't been defined and <c>required</c> is <see langword="false"/> nothing is rendered.<br />
		/// If the section has been defined it is rendered HTML-escaped, unless you use the triple-mustache: <c>{{{rendersection ...}}}</c>.
		/// </remarks>
		/// <example>
		/// <code title="view.hbs" language="none">
		/// <![CDATA[
		/// {{!< default}}
		/// {{#definesection "sidebar"}}
		///	    For more info, see<br />
		///	    <a href="link1.html">Link 1</a><br />
		///     <a href="link2.html">Link 2</a><br />
		/// {{/definesection}}
		/// ]]>
		/// </code>
		/// <code title="~/Views/_Layouts/default.hbs" language="none">
		/// <![CDATA[
		/// {{#if (issectiondefined "sidebar")}}
		///	    <div id="sidebar">
		///	        {{{rendersection "sidebar"}}}
		///	    </div>
		/// {{else}}
		///	    <p>Default sidebar content...</p>
		/// {{/if}}
		/// ]]>
		/// </code>
		/// </example>
		[SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "RenderSection")]
		public static void RenderSection(TextWriter writer, dynamic context, params object[] arguments)
		{
			// Render the named section (typically used in the layout)

			if(writer == null)
				throw new ArgumentNullException("writer");

			if(context == null)
				throw new ArgumentNullException("context");

			if(arguments == null)
				throw new ArgumentNullException("arguments");

			if(arguments.Length == 1 || arguments.Length == 2)
			{
				string name = arguments[0] as string;
				bool required = true;

				if(string.IsNullOrEmpty(name))
					throw new HelperException("RenderSection: must be called with a section name.");

				IDictionary<string,object> hash = null;

				if(arguments.Length == 2)
				{
					hash = arguments[1] as IDictionary<string, object>;

					if(hash == null)
					{
						throw new HelperException("RenderSection: Called with wrong arguments for section '"+name+"'.");
					}

					if(hash.Count != 1)
						throw new HelperException("RenderSection: Only one attribute is allowed and that is 'required=<bool>'");

					if(hash.ContainsKey("required"))
					{
						if(hash["required"] is bool)
						{
							required = (bool) hash["required"];
						}
						else
							throw new HelperException("RenderSection: Attribute 'required' must be boolean true/false but was " + hash["required"].GetType() + " '" + hash["required"] + "' (for section '" + name + "')");
					}
				}

				string keyname = SectionsPrefix + name;

				if(context.ContainsKey(keyname))
				{
					string html = context[keyname] as string;

					writer.Write(html); // Not WriteSafeString() - depend on the user using triple-mustache if she wants unescaped output.
				}
				else if(required)
				{
					throw new HelperException("RenderSection: Section '" + name + "' has not been defined but is required.");
				}
			}
			else
				throw new HelperException("RenderSection: must be called with a section name and possibly 'required=<boolean>'.");
		}
	}
}
