using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

// This file has the helpers that behaves like the Url helpers in ASP.NET MVC (Razor or Web Forms)
// Note: namespace doesn't end in ".Helpers" despite being in a Helpers folder.

namespace HandlebarsDotNet.Mvc
{
	public static partial class Helpers
	{
		// Example: {{url_action controller="Home" action="Index"}} = "/"
		/// <summary>
		/// Generates a fully qualified URL to an action method.
		/// </summary>
		/// <param name="writer">The TextWriter provided by HandlebarsDotNet</param>
		/// <param name="context">The context (model) provided by HandlebarsDotNet</param>
		/// <param name="arguments">The arguments from the view, provided by HandlebarsDotNet</param>
		/// <remarks>
		/// This helper is among the ones registered if <see cref="HandlebarsViewEngine.RegisterMvcHelpers"/> is called. If so, it is registered as <c>url_action</c> but you can choose your own name for this:
		/// <code language="C#">
		/// var hbsve = new HandlebarsViewEngine();
		/// hbsve.RegisterHelper("url_action", HandlebarsDotNet.Mvc.Helpers.UrlAction);
		/// </code>
		/// It works like <see href="https://msdn.microsoft.com/en-us/library/system.web.mvc.urlhelper.action.aspx"/>.
		/// <para/>
		/// <h1><b>Usage</b></h1>
		/// <c>{{url_action [controller=...] [action=...] [protocol=...] [host=...] [fragment=...] [key=value ...]}}</c>
		/// <h2>Arguments</h2>
		/// <list type="definition">
		///	<item>
		///		<term>controller</term>
		///		<description>string (optional) - The name of the controller (without "Controller").</description>
		///	</item>
		///	<item>
		///		<term>action</term>
		///		<description>string (optional) - The name of the action.</description>
		///	</item>
		///	<item>
		///		<term>protocol</term>
		///		<description>string (optional) - The protocol for the URL, such as "http" or "https".</description>
		///	</item>
		///	<item>
		///		<term>host</term>
		///		<description>string (optional) - The host name for the URL.</description>
		///	</item>
		///	<item>
		///		<term>fragment</term>
		///		<description>string (optional) - The "fragment" for the URL (a named anchor).</description>
		///	</item>
		///	<item>
		///		<term>the rest</term>
		///		<description>(optional) - The rest of the attributes are used as route values.</description>
		///	</item>
		/// </list>
		/// <h2>Description</h2>
		/// This helper generates a URL from the routing table using the specified arguments.<br />
		/// The URL that is rendered has a format like the following:<br />
		/// <br />
		/// /Home/About<br />
		/// <br />
		/// If special characters in the URL must be encoded, use the <see cref="UrlEncode">url_encode</see> helper. For the previous example, the <see cref="UrlEncode">url_encode</see> helper renders the following string:<br />
		/// <br />
		/// %2fHome%2fAbout
		/// </remarks>
		/// <example>
		/// This example assumes this helper has been registered as "url_action", the application is installed in the web root, and that the default routes are used:
		/// <code title="view.hbs">
		/// {{url_action controller="Home" action="Index"}}
		/// {{url_action controller="Home" action="Contact" fragment="fragment" id="support" extra="greetings"}}
		/// {{url_action controller="Account" action="Login"}}
		/// {{url_encode (url_action controller="Account" action="Login")}}
		/// </code>
		/// <code title="Renders">
		/// /
		/// /Home/Contact/support?extra=greetings#fragment
		/// /Account/Login
		/// %2fAccount%2fLogin
		/// </code>
		/// </example>
		[SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "UrlAction")]
		[SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "context", Justification = "This is part of the signature for HandlebarsDotNet helpers.")]
		public static void UrlAction(TextWriter writer, dynamic context, params object[] arguments)
		{
			if(writer == null)
				throw new ArgumentNullException("writer");

			if(arguments == null)
				throw new ArgumentNullException("arguments");

			if(arguments.Length == 1)
			{
				var hash = arguments[0] as Dictionary<string, object>;

				if(hash != null)
				{
					string routeName  = null; // Url.Action() doesn't support routeName so I don't either.
					string controller = GetAndRemove(hash, "controller") as string;
					string action     = GetAndRemove(hash, "action")     as string;
					string protocol   = GetAndRemove(hash, "protocol")   as string;
					string hostName   = GetAndRemove(hash, "host")       as string;
					string fragment   = GetAndRemove(hash, "fragment")   as string;

					RequestContext requestContext = HttpContext.Current.Request.RequestContext;

					var routeValues = MergeDictionaries(hash, requestContext.RouteData.Values);

					string url = UrlHelper.GenerateUrl(routeName, action, controller, protocol, hostName, fragment, routeValues, RouteTable.Routes, requestContext, includeImplicitMvcValues: false);

					writer.Write(url);	// Not WriteSafeString() - depend on the user using triple-mustache if she wants unescaped output.
				}
				else
					throw new HelperException("UrlAction: Any arguments to this must be in attribute form (key=value)");
			}
			else
				throw new HelperException("UrlAction: Arguments must be in attribute form (key=value)");
		}
		private static object GetAndRemove(IDictionary<string, object> dictionary, string key)
		{
			object val = null;
			if(dictionary.ContainsKey(key))
			{
				val = dictionary[key];
				dictionary.Remove(key);
			}
			return val;
		}
		private static RouteValueDictionary MergeDictionaries(params IDictionary<string,object>[] dictionaries)
		{
			var rvd = new RouteValueDictionary();

			foreach(var dictionary in dictionaries.Where(d => d != null))
			{
				foreach(KeyValuePair<string, object> kvp in dictionary)
				{
					if(!rvd.ContainsKey(kvp.Key))
					{
						rvd.Add(kvp.Key, kvp.Value);
					}
				}
			}
			return rvd;
		}

		// Example: {{url_content "~"}} = "", so it can be used like this: <link href="{{url_content "~"}}/favicon.ico" rel="shortcut icon" type="image/x-icon" />
		// Example: {{url_content "~/"}} = "/"
		// Example: {{url_content "~/favicon.ico"}} = "/favicon.ico"
		// Example: {{url_content "~user/"}} = "~user/"
		/// <summary>
		/// Converts a virtual (relative) path to an application absolute path and renders the result.
		/// </summary>
		/// <param name="writer">The TextWriter provided by HandlebarsDotNet</param>
		/// <param name="context">The context (model) provided by HandlebarsDotNet</param>
		/// <param name="arguments">The arguments from the view, provided by HandlebarsDotNet</param>
		/// <remarks>
		/// This helper is among the ones registered if <see cref="HandlebarsViewEngine.RegisterMvcHelpers"/> is called. If so, it is registered as <c>url_content</c> but you can choose your own name for this:
		/// <code language="C#">
		/// var hbsve = new HandlebarsViewEngine();
		/// hbsve.RegisterHelper("url_content", HandlebarsDotNet.Mvc.Helpers.UrlContent);
		/// </code>
		/// It works like <see href="https://msdn.microsoft.com/en-us/library/system.web.mvc.urlhelper.content.aspx"/> but with special cases (see below).
		/// <para/>
		/// <h1><b>Usage</b></h1>
		/// <c>{{url_content [path]}}</c>
		/// <h2>Arguments</h2>
		/// string [path] (required) - A virtual path
		/// <h2>Description</h2>
		/// This helper deviates from the one used in UrlHelper in the following ways:<br />
		/// If the path is exactly "~" it renders the path to the virtual directory (UrlHelper renders with an ending slash), and<br />
		/// if it starts with "~" but not "~/" it renders the path unchanged (UrlHelper generates an argument exception for this case).<br />
		/// If it does start with "~/" the path is converted to an application absolute path (just like the UrlHelper one) and rendered.<br />
		/// Just like the UrlHelper helper it renders the argument unchanged if it doesn't start with "~".
		/// <para>
		/// What this means is that if the path to this helper ends with a slash the rendered output will also do that.<br />
		/// If the path has the resemblance of a Linux user directory (~user) it will be unchanged, otherwise if it does start with "~" then the tilde is
		/// converted to the virtual directory where the web application is installed (usually the root "/").<br />
		/// If it doesn't start with "~" it will be rendered unchanged.
		/// </para>
		/// </remarks>
		/// <example>
		/// This example assumes this helper has been registered as "url_content" and the application is installed in the web root.
		/// <code title="view.hbs">
		/// "~"             = {{url_content "~"}}
		/// "~/"            = {{url_content "~/"}}
		/// "~/favicon.ico" = {{url_content "~/favicon.ico"}}
		/// "~user/"        = {{url_content "~user/"}}
		/// </code>
		/// <code title="Renders">
		/// "~"             = 
		/// "~/"            = /
		/// "~/favicon.ico" = /favicon.ico
		/// "~user/"        = ~user/
		/// </code>
		/// </example>
		[SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "UrlContent")]
		[SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "context", Justification = "This is part of the signature for HandlebarsDotNet helpers.")]
		public static void UrlContent(TextWriter writer, dynamic context, params object[] arguments)
		{
			if(writer == null)
				throw new ArgumentNullException("writer");

			if(arguments == null)
				throw new ArgumentNullException("arguments");

			if( (arguments.Length == 1) && (arguments[0].GetType() == typeof(string)) )
			{
				// There must be one argument, and it must be a string

				string tildepath = arguments[0] as string;

				if(string.IsNullOrEmpty(tildepath))
					throw new HelperException("UrlContent: the string is empty");

				string path;

				if(tildepath[0] == '~')
				{
					if(tildepath == "~")
					{
						// This is a special case...
						// Note: This behavior is not in standard ASP.NET MVC.

						path = VirtualPathUtility.ToAbsolute(tildepath);
						path = VirtualPathUtility.RemoveTrailingSlash(path);

						if(path == "/")
							path = string.Empty;
					}
					else if(tildepath.StartsWith("~/"))
					{
						path = VirtualPathUtility.ToAbsolute(tildepath);
					}
					else
					{
						// starts with "~" but doesn't start with "~/" (for example "~user/")
						// Use the path as specified. Note: This behavior is not in standard ASP.NET MVC.

						path = tildepath;
					}
				}
				else
				{
					// it didn't start with "~" so render the path as specified

					path = tildepath;
				}

				writer.Write(path);		// Not WriteSafeString() - depend on the user using triple-mustache if she wants unescaped output.
			}
			else
				throw new HelperException("UrlContent: Expecting one and only one string");
		}

		// Example: {{url_encode "/Home/About"}} = "%2fHome%2fAbout"
		/// <summary>
		/// Encodes text so it is usable in a URL.
		/// </summary>
		/// <param name="writer">The TextWriter provided by HandlebarsDotNet</param>
		/// <param name="context">The context (model) provided by HandlebarsDotNet</param>
		/// <param name="arguments">The arguments from the view, provided by HandlebarsDotNet</param>
		/// <remarks>
		/// This helper is among the ones registered if <see cref="HandlebarsViewEngine.RegisterMvcHelpers"/> is called. If so, it is registered as <c>url_encode</c> but you can choose your own name for this:
		/// <code language="C#">
		/// var hbsve = new HandlebarsViewEngine();
		/// hbsve.RegisterHelper("url_encode", HandlebarsDotNet.Mvc.Helpers.UrlEncode);
		/// </code>
		/// It works like <see href="https://msdn.microsoft.com/en-us/library/system.web.mvc.urlhelper.encode.aspx"/>.
		/// <para/>
		/// <h1><b>Usage</b></h1>
		/// <c>{{url_encode [str]}}</c>
		/// <h2>Arguments</h2>
		/// string [str] (required) - The string to render URL encoded.
		/// <h2>Description</h2>
		/// </remarks>
		/// <example>
		/// This example assumes this helper has been registered as "url_encode".
		/// <code title="view.hbs">
		/// {{url_encode "/Home/About"}}
		/// </code>
		/// <code title="Renders">
		/// %2fHome%2fAbout
		/// </code>
		/// </example>
		[SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "UrlEncode")]
		[SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "context", Justification = "This is part of the signature for HandlebarsDotNet helpers.")]
		public static void UrlEncode(TextWriter writer, dynamic context, params object[] arguments)
		{
			if(writer == null)
				throw new ArgumentNullException("writer");

			if(arguments == null)
				throw new ArgumentNullException("arguments");

			if((arguments.Length == 1) && (arguments[0].GetType() == typeof(string)))
			{
				string url = arguments[0] as string;

				// Being called with an empty string is not an error

				if(!string.IsNullOrEmpty(url))
				{
					writer.WriteSafeString(HttpUtility.UrlEncode(url));
				}
			}
			else
				throw new HelperException("UrlEncode: Expecting one and only one string");
		}
	}
}