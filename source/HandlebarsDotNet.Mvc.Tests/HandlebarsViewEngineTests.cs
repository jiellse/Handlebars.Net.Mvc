using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using HandlebarsDotNet.Mvc.Tests.TestInternal;
using Moq;
using Xunit;

namespace HandlebarsDotNet.Mvc.Tests
{
	public class HandlebarsViewEngineTests
	{
		[Fact]
		public void Constructor_doesnt_throw()
		{
			new HandlebarsViewEngine();
		}

		[Fact]
		public void HandlebarsConfiguration_is_usable()
		{
			var hbsve = new HandlebarsViewEngine();

			hbsve.HandlebarsConfiguration.Helpers.Add("link_to", (writer, context, parameters) =>
			{
				string html = "<a href='" + context.url + "'>" + context.text + "</a>";
				writer.WriteSafeString(html);
			});
		}

#if false
		// This threw an exception in Handlebars.Net 1.8.0
		// It doesn't anymore in v1.9.0
		[Fact]
		public void Handlebars_1_8_throws_when_compiling_mustache_as_part_of_string()
		{
			var ex = Assert.Throws<HandlebarsCompilerException>(() =>
			{
				var func = Handlebars.Compile("{{formatvalue pi '{0:d}'}}");
			});
			Assert.Equal("Starting and ending handlebars do not match", ex.Message);
		}
#endif
		[Fact]
		public void Handlebars_1_9_doesnt_throw_when_compiling_mustache_as_part_of_string()
		{
			var vpp = new VPP(
				new VPP.Dir("Views",
					new VPP.Dir("Home",
						new VPP.File("index.hbs", "Pi is about {{formatvalue pi '{0:N}'}}")
						)
					)
				);
			var hbsve = new HandlebarsViewEngine();
			hbsve.VirtualPathProvider = vpp;

			hbsve.RegisterHelper("formatvalue", (writer, context, args) =>
			{
				object val    = args[0];
				string format = args[1] as string;
				ViewContext viewContext = HandlebarsView.GetViewContext(context);
				HtmlHelper htmlHelper = new HtmlHelper(viewContext, viewContext.View as HandlebarsView);
				string formatted = htmlHelper.FormatValue(val, format);
				writer.Write(formatted);
			});

			var httpContext = new Mock<HttpContextBase>();
			var controller = new Mock<ControllerBase>();
			var routeData = new RouteData();
			routeData.Values.Add("controller", "Home");
			var controllerContext = new ControllerContext(httpContext.Object, routeData, controller.Object);

			var viewengineResult = hbsve.FindView(controllerContext, viewName: "index", masterName: null, useCache: false);
			var hbsview = viewengineResult.View;

			string actual = GetHtmlFromView(hbsview, new
			{
				pi = 3.14159265358979
			});

			Assert.Equal("Pi is about 3.14", actual);
		}

		public class RegisterHelper
		{
			class TestViewEngine : HandlebarsViewEngine
			{
				public TestViewEngine()
				{
					var vpp = new VPP(
						new VPP.Dir("Views",
							new VPP.Dir("_Shared",
								new VPP.File("index.hbs", "Name: {{name}}")
								)
							)
						);

					this.VirtualPathProvider = vpp;
				}
			}

			[Fact]
			public void Registering_helper_doesnt_pollute_global_Handlebars()
			{
				var hbsve = new HandlebarsViewEngine();

				hbsve.RegisterHelper("name", (writer, context, args) =>
				{
					writer.Write("Rendered by helper in HandlebarsViewEngine");
				});

				// compile with global Handlebars
				var func = Handlebars.Compile("Name: {{name}}");

				string output = func(new {});

				Assert.Equal("Name: ", output);
			}

			[Fact]
			public void Registering_helper_makes_it_available_to_all_views()
			{
				var hbsve = new TestViewEngine();

				hbsve.RegisterHelper("name", (writer, context, args) =>
				{
					writer.Write("Rendered by helper in HandlebarsViewEngine");
				});

				var httpContext = new Mock<HttpContextBase>();
				var controller = new Mock<ControllerBase>();
				var routeData = new RouteData();
				routeData.Values.Add("controller", "Blog");
				var controllerContext = new ControllerContext(httpContext.Object, routeData, controller.Object);

				var viewengineResult = hbsve.FindView(controllerContext, viewName: "index", masterName: null, useCache: false);
				var hbsview = viewengineResult.View;

				string actual = GetHtmlFromView(hbsview, new {});

				Assert.Equal("Name: Rendered by helper in HandlebarsViewEngine", actual);
			}
		}

		public class Partials
		{
			[Fact]
			public void Partials_are_scoped_to_controller()
			{
				var vpp = new VPP(
					new VPP.Dir("Views",
						new VPP.Dir("Blog",
							new VPP.Dir("_Partials",
								new VPP.File("person.hbs", "Blog's partial")
								)
							),
						new VPP.Dir("_Partials",
							new VPP.File("person.hbs", "Global partial")
							),
						new VPP.Dir("_Shared",
							new VPP.File("index.hbs", "Hello, {{>person}}!")
							)
						)
					);
				var hbsve = new HandlebarsViewEngine();
				hbsve.VirtualPathProvider = vpp;

				var httpContext = new Mock<HttpContextBase>();
				var controller = new Mock<ControllerBase>();
				var routeData = new RouteData();
				routeData.Values.Add("controller", "Blog");
				var controllerContext = new ControllerContext(httpContext.Object, routeData, controller.Object);

				var viewengineResult = hbsve.FindView(controllerContext, viewName: "index", masterName: null, useCache: false);
				var hbsview = viewengineResult.View;

				string actual = GetHtmlFromView(hbsview, new {});

				Assert.Equal("Hello, Blog's partial!", actual);

				// Redo the test, now for the HomeController
				routeData.Values["controller"] = "Home";
				controllerContext = new ControllerContext(httpContext.Object, routeData, controller.Object);
				viewengineResult = hbsve.FindView(controllerContext, viewName: "index", masterName: null, useCache: false);
				hbsview = viewengineResult.View;
				actual = GetHtmlFromView(hbsview, new {});
				Assert.Equal("Hello, Global partial!", actual);
			}

			[Fact]
			public void BasicPartialWithStringParametersAndImplicitContext()
			{
				// The same test as the same named in https://github.com/rexm/Handlebars.Net/blob/master/source/Handlebars.Test/PartialTests.cs,
				// only using a VirtualPathProvider instead of source strings.

				var vpp = new VPP(
					new VPP.Dir("Views",
						new VPP.Dir("_Partials",
							new VPP.File("person.hbs", "{{firstName}} {{lastName}}")
							),
						new VPP.Dir("_Shared",
							new VPP.File("index.hbs", "Hello, {{>person lastName='Smith'}}!")
							)
						)
					);
				var hbsve = new HandlebarsViewEngine();
				hbsve.VirtualPathProvider = vpp;

				var data = new 
				{
					firstName = "Marc",
					lastName = "Jones"
				};

				var httpContext = new Mock<HttpContextBase>();
				var controller = new Mock<ControllerBase>();
				var routeData = new RouteData();
				routeData.Values.Add("controller", "Blog");
				var controllerContext = new ControllerContext(httpContext.Object, routeData, controller.Object);

				var viewengineResult = hbsve.FindView(controllerContext, viewName: "index", masterName: null, useCache: false);
				var hbsview = viewengineResult.View;

				string actual = GetHtmlFromView(hbsview, data);

				Assert.Equal("Hello, Marc Smith!", actual);
			}
		}

		private static string GetHtmlFromView(IView hbsview, dynamic model)
		{
			string html = null;

			using(var textWriter = new StringWriter())
			{
				var controllerContext = new ControllerContext();
				ViewContext viewContext = new ViewContext(controllerContext, hbsview, new ViewDataDictionary(model), tempData: new TempDataDictionary(), writer: textWriter);

				hbsview.Render(viewContext, textWriter);
				html = textWriter.GetStringBuilder().ToString();
			}

			return html;
		}
	}
}
