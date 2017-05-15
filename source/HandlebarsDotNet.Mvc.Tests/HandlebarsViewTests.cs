using System;
using System.Web.Mvc;
using Xunit;
using HandlebarsDotNet;
using System.IO;
using System.Dynamic;
using System.Collections.Generic;

// http://xunit.github.io/docs/shared-context.html

namespace HandlebarsDotNet.Mvc.Tests
{
	public class HandlebarsViewTests
	{
		// This is created once, before any of the tests are run
		public class HandlebarsFixture
		{
			public HandlebarsFixture()
			{
				this.Handlebars = HandlebarsDotNet.Handlebars.Create();

				var funcView         = this.Handlebars.Compile("Hello {{user}}!");
				var funcViewViewdata = this.Handlebars.Compile("Title: {{viewbag.Title}}. Hello, {{FirstName}} {{LastName}}!");
				var funcLayout       = this.Handlebars.Compile("<html>{{{body}}}</html>");

				this.ViewNoLayout      = new CompiledView(funcView,         fileHash: null, layout: null);
				this.ViewWithLayout    = new CompiledView(funcView,         fileHash: null, layout: "layoutname");
				this.ViewUsingViewdata = new CompiledView(funcViewViewdata, fileHash: null, layout: null);
				this.LayoutView        = new CompiledView(funcLayout,       fileHash: null, layout: null);
			}

			public IHandlebars  Handlebars        { get; private set; }
			public CompiledView ViewNoLayout      { get; private set; }
			public CompiledView ViewWithLayout    { get; private set; }
			public CompiledView ViewUsingViewdata { get; private set; }
			public CompiledView LayoutView        { get; private set; }
		}

		//

		public class Layouts : IClassFixture<HandlebarsFixture>
		{
			HandlebarsFixture _fixture;
			ControllerContext _controllerContext;
			dynamic           _model;

			public Layouts(HandlebarsFixture fixture)
			{
				_fixture = fixture;
				_controllerContext = new ControllerContext();

				_model = new
				{
					user = "World"
				};
			}

			private string GetHtmlFromView(HandlebarsView hbsview)
			{
				string html = null;

				using(var textWriter = new StringWriter())
				{
					ViewContext viewContext = new ViewContext(_controllerContext, hbsview, new ViewDataDictionary(_model), tempData: new TempDataDictionary(), writer: textWriter);

					hbsview.Render(viewContext, textWriter);
					html = textWriter.GetStringBuilder().ToString();
				}

				return html;
			}

			// no layouts specified - only renders (layout-less) view
			[Fact]
			public void NoLayoutOnlyRendersView()
			{
				var hbsview = new HandlebarsView(_controllerContext, _fixture.ViewNoLayout, layouts: null);

				string html = GetHtmlFromView(hbsview);

				Assert.Equal("Hello World!", html);
			}

			// no layouts specified - only renders view even if view specifies layout
			[Fact]
			public void NoLayoutOnlyRendersViewEvenIfViewSpecifiesLayout()
			{
				var hbsview = new HandlebarsView(_controllerContext, _fixture.ViewWithLayout, layouts: null);

				string html = GetHtmlFromView(hbsview);

				Assert.Equal("Hello World!", html);
			}

			// layout specified - renders layout-less view and layout
			[Fact]
			public void LayoutSpecifiedRendersLayoutlessViewAndLayout()
			{
				var hbsview = new HandlebarsView(_controllerContext, _fixture.ViewNoLayout, layouts: new CompiledView[] { _fixture.LayoutView });

				string html = GetHtmlFromView(hbsview);

				Assert.Equal("<html>Hello World!</html>", html);
			}

			// layout specified - renders view and layout specified (not view's layout)
			[Fact]
			public void LayoutSpecifiedRendersViewAndLayout()
			{
				var hbsview = new HandlebarsView(_controllerContext, _fixture.ViewWithLayout, layouts: new CompiledView[] { _fixture.LayoutView });

				string html = GetHtmlFromView(hbsview);

				Assert.Equal("<html>Hello World!</html>", html);
			}

			// layouts specified - renders view and layouts specified (not view's layout)
			[Fact]
			public void LayoutsSpecifiedRendersViewAndSeveralLayouts()
			{
				var hbsview = new HandlebarsView(_controllerContext, _fixture.ViewWithLayout, layouts: new CompiledView[] { _fixture.LayoutView, _fixture.LayoutView });

				string html = GetHtmlFromView(hbsview);

				Assert.Equal("<html><html>Hello World!</html></html>", html);
			}

			[Fact]
			public void MimicRealWorld()
			{
				var hbsview = new HandlebarsView(_controllerContext, _fixture.ViewUsingViewdata, layouts: null);

				ViewDataDictionary vdd = new ViewDataDictionary();

				vdd["title"] = "Greetings";

				vdd.Model = new Person()
				{
					FirstName = "John",
					LastName  = "Doe"
				};
				// The lines above are equivalent to (in a controller):
				//   ViewBag.title = "Greetings";
				//   var model = new Person
				//   {
				//       FirstName = "John",
				//       LastName  = "Doe"
				//   };
				//   return View(model);

				string html;
				using(var textWriter = new StringWriter())
				{
					ViewContext viewContext = new ViewContext(_controllerContext, hbsview, vdd, tempData: new TempDataDictionary(), writer: textWriter);

					hbsview.Render(viewContext, textWriter);
					html = textWriter.GetStringBuilder().ToString();
				}

				Assert.Equal("Title: Greetings. Hello, John Doe!", html);
			}

			public class Person
			{
				public string FirstName { get; set; }
				public string LastName  { get; set; }
			}
		}

		public class Models
		{
			IHandlebars _handlebars;
			CompiledView _compiledView;
			ViewDataDictionary _vdd;
			ControllerContext _controllerContext;
			HandlebarsView _hbsview;

			public Models()
			{
				_controllerContext = new ControllerContext();

				_handlebars = HandlebarsDotNet.Handlebars.Create();

				_vdd = new ViewDataDictionary();

				// The view. Note that we're also testing for case-insensitivity (lastname is lowercase here, but not in the models).
				var funcView = _handlebars.Compile("Hello, {{FirstName}} {{lastname}}!");

				_compiledView = new CompiledView(funcView, fileHash: null, layout: null);

				_hbsview = new HandlebarsView(_controllerContext, _compiledView, layouts: null);

			}

			private string GetHtmlFromView()
			{
				string html = null;
				using(var textWriter = new StringWriter())
				{
					ViewContext viewContext = new ViewContext(_controllerContext, _hbsview, _vdd, tempData: new TempDataDictionary(), writer: textWriter);
					_hbsview.Render(viewContext, textWriter);
					html = textWriter.GetStringBuilder().ToString();
				}
				return html;
			}

			[Fact]
			public void UsingAnonymousObject()
			{
				_vdd.Model = new { firstName = "John", LastName = "Doe" };
				string html = GetHtmlFromView();
				Assert.Equal("Hello, John Doe!", html);
			}

			[Fact]
			public void UsingClassWithProperties()
			{
				_vdd.Model = new PersonClassWithProperties("John", "Doe");
				string html = GetHtmlFromView();
				Assert.Equal("Hello, John Doe!", html);
			}

			[Fact]
			public void UsingClassWithFields()
			{
				_vdd.Model = new PersonClassWithFields("John", "Doe");
				string html = GetHtmlFromView();
				Assert.Equal("Hello, John Doe!", html);
			}

			[Fact]
			public void UsingStruct()
			{
				_vdd.Model = new PersonStruct { FirstName = "John", LastName = "Doe" };
				string html = GetHtmlFromView();
				Assert.Equal("Hello, John Doe!", html);
			}

			[Fact]
			public void UsingDictionaryStringKeys()
			{
				var dict = new Dictionary<string, object>();
				dict.Add("FirstName", "John");
				dict.Add("LastName","Doe");
				_vdd.Model = dict;
				string html = GetHtmlFromView();
				Assert.Equal("Hello, John Doe!", html);
				Assert.Equal(2, dict.Keys.Count);	// making sure the added properties in HandlebarsView weren't added to the original model
			}

			[Fact]
			public void UsingDeepObjectAndDictionaryIntKeys()
			{
				var dict = new Dictionary<int, object>();
				dict.Add( 0, "Romeo");
				dict.Add(42, "Tango");
				dict.Add( 9, "Foxtrot");
				dict.Add(13, "Mike");
				_vdd.Model = new
				{
					firstname = "John",
					lastname = "Doe",
					Dict = dict
				};
				// overwriting the ones set in this test class' constructor
				var funcView = _handlebars.Compile("Hello, {{firstName}} {{Lastname}}!\n{{#each Dict}} {{@value}}{{/each}}");
				_compiledView = new CompiledView(funcView, fileHash: null, layout: null);
				_hbsview = new HandlebarsView(_controllerContext, _compiledView, layouts: null);
				// end overwriting
				string html = GetHtmlFromView();
				Assert.Equal("Hello, John Doe!\n Romeo Tango Foxtrot Mike", html);
				Assert.Equal(4, dict.Keys.Count);	// making sure the added properties in HandlebarsView weren't added to the dictionary
			}

			class PersonClassWithProperties
			{
				public PersonClassWithProperties(string firstname, string lastname)
				{
					FirstName = firstname;
					LastName = lastname;
				}
				public string FirstName { get; private set; }
				public string LastName  { get; private set; }
			}
			class PersonClassWithFields
			{
				public PersonClassWithFields(string firstname, string lastname)
				{
					FirstName = firstname;
					LastName = lastname;
				}
				public string FirstName;
				public string LastName;
			}
			struct PersonStruct
			{
				public string FirstName;
				public string LastName;
			}
		}
	}
}
