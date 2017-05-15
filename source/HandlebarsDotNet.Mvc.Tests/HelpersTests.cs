using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using HandlebarsDotNet.Mvc.Tests.TestInternal;
using Xunit;

namespace HandlebarsDotNet.Mvc.Tests
{
	public class HelpersTests
	{
		public class Sections
		{
			private string GetHtmlFromView(HandlebarsView hbsview)
			{
				string html = null;

				using(var textWriter = new StringWriter())
				{
					var controllerContext = new ControllerContext();
					ViewContext viewContext = new ViewContext(controllerContext, hbsview, new ViewDataDictionary(), tempData: new TempDataDictionary(), writer: textWriter);

					hbsview.Render(viewContext, textWriter);
					html = textWriter.GetStringBuilder().ToString();
				}

				return html;
			}

			[Fact]
			public void RenderSection_throws_for_undefined_section_when_required()
			{
				var hbsve = new HandlebarsViewEngine();
				hbsve.RegisterSectionsHelpers();
				var handlebars = Handlebars.Create(hbsve.HandlebarsConfiguration);

				var view   = new CompiledView(handlebars.Compile("Body not rendered."), null, null);
				var layout = new CompiledView(handlebars.Compile("Name: {{rendersection \"name\"}}."), null, null);

				var controllerContext = new ControllerContext();
				var hbsview = new HandlebarsView(controllerContext, view, new[] { layout });

				Assert.Throws<HelperException>(() =>
				{
					var html = GetHtmlFromView(hbsview);
				});
			}

			[Fact]
			public void RenderSection_doesnt_throw_for_undefined_section_when_not_required()
			{
				var hbsve = new HandlebarsViewEngine();
				hbsve.RegisterSectionsHelpers();
				var handlebars = Handlebars.Create(hbsve.HandlebarsConfiguration);

				var view   = new CompiledView(handlebars.Compile("Body not rendered."), null, null);
				var layout = new CompiledView(handlebars.Compile("Name: {{rendersection \"name\" required=false}}."), null, null);

				var controllerContext = new ControllerContext();
				var hbsview = new HandlebarsView(controllerContext, view, new[] { layout });

				var html = GetHtmlFromView(hbsview);
				Assert.Equal("Name: .", html);
			}

			[Fact]
			public void DefineSection_can_replace()
			{
				var hbsve = new HandlebarsViewEngine();
				hbsve.RegisterSectionsHelpers();
				var handlebars = Handlebars.Create(hbsve.HandlebarsConfiguration);

				var view   = new CompiledView(handlebars.Compile("{{#definesection \"name\"}}From view.{{/definesection}}"), null, null);
				var layout1 = new CompiledView(handlebars.Compile("{{#definesection \"name\" mode=\"replace\"}}Replaced.{{/definesection}}"), null, null);
				var layout2 = new CompiledView(handlebars.Compile("{{rendersection \"name\"}}"), null, null);

				var controllerContext = new ControllerContext();
				var hbsview = new HandlebarsView(controllerContext, view, new[] { layout1, layout2 });

				var html = GetHtmlFromView(hbsview);
				Assert.Equal("Replaced.", html);
			}

			[Fact]
			public void DefineSection_can_append()
			{
				var hbsve = new HandlebarsViewEngine();
				hbsve.RegisterSectionsHelpers();
				var handlebars = Handlebars.Create(hbsve.HandlebarsConfiguration);

				var view   = new CompiledView(handlebars.Compile("{{#definesection \"name\"}}From view.{{/definesection}}"), null, null);
				var layout1 = new CompiledView(handlebars.Compile("{{#definesection \"name\" mode=\"append\"}}Appended.{{/definesection}}"), null, null);
				var layout2 = new CompiledView(handlebars.Compile("{{rendersection \"name\"}}"), null, null);

				var controllerContext = new ControllerContext();
				var hbsview = new HandlebarsView(controllerContext, view, new[] { layout1, layout2 });

				var html = GetHtmlFromView(hbsview);
				Assert.Equal("From view.Appended.", html);
			}

			[Fact]
			public void DefineSection_can_prepend()
			{
				var hbsve = new HandlebarsViewEngine();
				hbsve.RegisterSectionsHelpers();
				var handlebars = Handlebars.Create(hbsve.HandlebarsConfiguration);

				var view   = new CompiledView(handlebars.Compile("{{#definesection \"name\"}}From view.{{/definesection}}"), null, null);
				var layout1 = new CompiledView(handlebars.Compile("{{#definesection \"name\" mode=\"prepend\"}}Prepended.{{/definesection}}"), null, null);
				var layout2 = new CompiledView(handlebars.Compile("{{rendersection \"name\"}}"), null, null);

				var controllerContext = new ControllerContext();
				var hbsview = new HandlebarsView(controllerContext, view, new[] { layout1, layout2 });

				var html = GetHtmlFromView(hbsview);
				Assert.Equal("Prepended.From view.", html);
			}

			string source =
				"{{#if (issectiondefined \"sidebar\")}}" +
				"<div id=\"sidebar\">{{{rendersection \"sidebar\"}}}</div>" +
				"{{else}}" +
				"<p>Default sidebar content...</p>" +
				"{{/if}}";

			[Fact]
			public void Sidebar_defined_renders_content_and_chrome()
			{
				var hbsve = new HandlebarsViewEngine();
				hbsve.RegisterSectionsHelpers();
				var handlebars = Handlebars.Create(hbsve.HandlebarsConfiguration);

				var view   = new CompiledView(handlebars.Compile("{{#definesection \"sidebar\"}}Sidebar{{/definesection}}"), null, null);
				var layout = new CompiledView(handlebars.Compile(source), null, null);

				var controllerContext = new ControllerContext();
				var hbsview = new HandlebarsView(controllerContext, view, new[] { layout });

				var html = GetHtmlFromView(hbsview);
				Assert.Equal("<div id=\"sidebar\">Sidebar</div>", html);
			}

			[Fact]
			public void Sidebar_not_defined_renders_default_content()
			{
				var hbsve = new HandlebarsViewEngine();
				hbsve.RegisterSectionsHelpers();
				var handlebars = Handlebars.Create(hbsve.HandlebarsConfiguration);

				var view   = new CompiledView(handlebars.Compile("No section here."), null, null);
				var layout = new CompiledView(handlebars.Compile(source), null, null);

				var controllerContext = new ControllerContext();
				var hbsview = new HandlebarsView(controllerContext, view, new[] { layout });

				var html = GetHtmlFromView(hbsview);
				Assert.Equal("<p>Default sidebar content...</p>", html);
			}
		}
	}
}
