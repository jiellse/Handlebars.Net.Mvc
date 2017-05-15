using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Moq;
using Xunit;

namespace HandlebarsDotNet.Mvc.Tests
{
	public class DefaultPathsProviderTests
	{
		public class GetViewFolders
		{
			[Fact]
			public void HomeController()
			{
				var httpContext = new Mock<HttpContextBase>();
				var controller = new Mock<ControllerBase>();
				var routeData = new RouteData();
				routeData.Values.Add("controller", "Home");
				var controllerContext = new ControllerContext(httpContext.Object, routeData, controller.Object);

				var dpp = new DefaultPathsProvider();
				var paths = dpp.GetViewFolders(controllerContext);

				var expected = new string[] {
					"~/Views/Home/",
					"~/Views/_Shared/"
				};
				Assert.Equal(expected, paths.ToArray());
			}

			[Fact]
			public void BlogController()
			{
				var httpContext = new Mock<HttpContextBase>();
				var controller = new Mock<ControllerBase>();
				var routeData = new RouteData();
				routeData.Values.Add("controller", "Blog");
				var controllerContext = new ControllerContext(httpContext.Object, routeData, controller.Object);

				var dpp = new DefaultPathsProvider();
				var paths = dpp.GetViewFolders(controllerContext);

				var expected = new string[] {
					"~/Views/Blog/",
					"~/Views/_Shared/"
				};
				Assert.Equal(expected, paths.ToArray());
			}

			[Fact]
			public void HomeController_in_area_Admin()
			{
				var httpContext = new Mock<HttpContextBase>();
				var controller = new Mock<ControllerBase>();
				var routeData = new RouteData();
				routeData.Values.Add("controller", "Home");
				routeData.DataTokens.Add("area", "Admin");
				var controllerContext = new ControllerContext(httpContext.Object, routeData, controller.Object);

				var dpp = new DefaultPathsProvider();
				var paths = dpp.GetViewFolders(controllerContext);

				var expected = new string[] {
					"~/Areas/Admin/Views/Home/",
					"~/Areas/Admin/Views/_Shared/",
					"~/Views/Home/",
					"~/Views/_Shared/"
				};
				Assert.Equal(expected, paths.ToArray());
			}

			[Fact]
			public void HomeController_AllowControllerlessViewFolder()
			{
				var httpContext = new Mock<HttpContextBase>();
				var controller = new Mock<ControllerBase>();
				var routeData = new RouteData();
				routeData.Values.Add("controller", "Home");
				var controllerContext = new ControllerContext(httpContext.Object, routeData, controller.Object);

				var dpp = new DefaultPathsProvider();
				dpp.AllowControllerlessViewFolder = true;
				var paths = dpp.GetViewFolders(controllerContext);

				var expected = new string[] {
					"~/Views/Home/",
					"~/Views/_Shared/",
					"~/Views/"
				};
				Assert.Equal(expected, paths.ToArray());
			}

			[Fact]
			public void HomeController_in_area_Admin_AllowControllerlessViewFolder()
			{
				var httpContext = new Mock<HttpContextBase>();
				var controller = new Mock<ControllerBase>();
				var routeData = new RouteData();
				routeData.Values.Add("controller", "Home");
				routeData.DataTokens.Add("area", "Admin");
				var controllerContext = new ControllerContext(httpContext.Object, routeData, controller.Object);

				var dpp = new DefaultPathsProvider();
				dpp.AllowControllerlessViewFolder = true;
				var paths = dpp.GetViewFolders(controllerContext);

				var expected = new string[] {
					"~/Areas/Admin/Views/Home/",
					"~/Areas/Admin/Views/_Shared/",
					"~/Areas/Admin/Views/",
					"~/Views/Home/",
					"~/Views/_Shared/",
					"~/Views/"
				};
				Assert.Equal(expected, paths.ToArray());
			}

			[Fact]
			public void HomeController_in_area_Admin_renamed_Views_folder()
			{
				var httpContext = new Mock<HttpContextBase>();
				var controller = new Mock<ControllerBase>();
				var routeData = new RouteData();
				routeData.Values.Add("controller", "Home");
				routeData.DataTokens.Add("area", "Admin");
				var controllerContext = new ControllerContext(httpContext.Object, routeData, controller.Object);

				var dpp = new DefaultPathsProvider();
				dpp.ViewsFolderName = "Renamed";
				var paths = dpp.GetViewFolders(controllerContext);

				var expected = new string[] {
					"~/Areas/Admin/Renamed/Home/",
					"~/Areas/Admin/Renamed/_Shared/",
					"~/Renamed/Home/",
					"~/Renamed/_Shared/"
				};
				Assert.Equal(expected, paths.ToArray());
			}

			[Fact]
			public void HomeController_in_area_Admin_renamed_Areas_folder()
			{
				var httpContext = new Mock<HttpContextBase>();
				var controller = new Mock<ControllerBase>();
				var routeData = new RouteData();
				routeData.Values.Add("controller", "Home");
				routeData.DataTokens.Add("area", "Admin");
				var controllerContext = new ControllerContext(httpContext.Object, routeData, controller.Object);

				var dpp = new DefaultPathsProvider();
				dpp.AreasFolderName = "Renamed";
				var paths = dpp.GetViewFolders(controllerContext);

				var expected = new string[] {
					"~/Renamed/Admin/Views/Home/",
					"~/Renamed/Admin/Views/_Shared/",
					"~/Views/Home/",
					"~/Views/_Shared/"
				};
				Assert.Equal(expected, paths.ToArray());
			}

			[Fact]
			public void HomeController_in_area_Admin_renamed_Shared_folder()
			{
				var httpContext = new Mock<HttpContextBase>();
				var controller = new Mock<ControllerBase>();
				var routeData = new RouteData();
				routeData.Values.Add("controller", "Home");
				routeData.DataTokens.Add("area", "Admin");
				var controllerContext = new ControllerContext(httpContext.Object, routeData, controller.Object);

				var dpp = new DefaultPathsProvider();
				dpp.SharedViewsFolderName = "Renamed";
				var paths = dpp.GetViewFolders(controllerContext);

				var expected = new string[] {
					"~/Areas/Admin/Views/Home/",
					"~/Areas/Admin/Views/Renamed/",
					"~/Views/Home/",
					"~/Views/Renamed/"
				};
				Assert.Equal(expected, paths.ToArray());
			}

			[Fact]
			public void HomeController_in_area_Admin_no_Shared_folder()
			{
				var httpContext = new Mock<HttpContextBase>();
				var controller = new Mock<ControllerBase>();
				var routeData = new RouteData();
				routeData.Values.Add("controller", "Home");
				routeData.DataTokens.Add("area", "Admin");
				var controllerContext = new ControllerContext(httpContext.Object, routeData, controller.Object);

				var dpp = new DefaultPathsProvider();
				dpp.SharedViewsFolderName = null;
				var paths = dpp.GetViewFolders(controllerContext);

				var expected = new string[] {
					"~/Areas/Admin/Views/Home/",
					"~/Views/Home/"
				};
				Assert.Equal(expected, paths.ToArray());
			}
		}

		public class GetLayoutFolders
		{
			[Fact]
			public void HomeController()
			{
				var httpContext = new Mock<HttpContextBase>();
				var controller = new Mock<ControllerBase>();
				var routeData = new RouteData();
				routeData.Values.Add("controller", "Home");
				var controllerContext = new ControllerContext(httpContext.Object, routeData, controller.Object);

				var dpp = new DefaultPathsProvider();
				var paths = dpp.GetLayoutFolders(controllerContext);

				var expected = new string[] {
					"~/Views/Home/_Layouts/",
					"~/Views/_Layouts/"
				};
				Assert.Equal(expected, paths.ToArray());
			}

			[Fact]
			public void BlogController()
			{
				var httpContext = new Mock<HttpContextBase>();
				var controller = new Mock<ControllerBase>();
				var routeData = new RouteData();
				routeData.Values.Add("controller", "Blog");
				var controllerContext = new ControllerContext(httpContext.Object, routeData, controller.Object);

				var dpp = new DefaultPathsProvider();
				var paths = dpp.GetLayoutFolders(controllerContext);

				var expected = new string[] {
					"~/Views/Blog/_Layouts/",
					"~/Views/_Layouts/"
				};
				Assert.Equal(expected, paths.ToArray());
			}

			[Fact]
			public void HomeController_in_area_Admin()
			{
				var httpContext = new Mock<HttpContextBase>();
				var controller = new Mock<ControllerBase>();
				var routeData = new RouteData();
				routeData.Values.Add("controller", "Home");
				routeData.DataTokens.Add("area", "Admin");
				var controllerContext = new ControllerContext(httpContext.Object, routeData, controller.Object);

				var dpp = new DefaultPathsProvider();
				var paths = dpp.GetLayoutFolders(controllerContext);

				var expected = new string[] {
					"~/Areas/Admin/Views/Home/_Layouts/",
					"~/Areas/Admin/Views/_Layouts/",
					"~/Views/Home/_Layouts/",
					"~/Views/_Layouts/"
				};
				Assert.Equal(expected, paths.ToArray());
			}

			[Fact]
			public void HomeController_AllowControllerlessViewFolder()
			{
				var httpContext = new Mock<HttpContextBase>();
				var controller = new Mock<ControllerBase>();
				var routeData = new RouteData();
				routeData.Values.Add("controller", "Home");
				var controllerContext = new ControllerContext(httpContext.Object, routeData, controller.Object);

				var dpp = new DefaultPathsProvider();
				dpp.AllowControllerlessViewFolder = true;
				var paths = dpp.GetLayoutFolders(controllerContext);

				var expected = new string[] {
					"~/Views/Home/_Layouts/",
					"~/Views/_Layouts/"
				};
				Assert.Equal(expected, paths.ToArray());
			}

			[Fact]
			public void HomeController_in_area_Admin_AllowControllerlessViewFolder()
			{
				var httpContext = new Mock<HttpContextBase>();
				var controller = new Mock<ControllerBase>();
				var routeData = new RouteData();
				routeData.Values.Add("controller", "Home");
				routeData.DataTokens.Add("area", "Admin");
				var controllerContext = new ControllerContext(httpContext.Object, routeData, controller.Object);

				var dpp = new DefaultPathsProvider();
				dpp.AllowControllerlessViewFolder = true;
				var paths = dpp.GetLayoutFolders(controllerContext);

				var expected = new string[] {
					"~/Areas/Admin/Views/Home/_Layouts/",
					"~/Areas/Admin/Views/_Layouts/",
					"~/Views/Home/_Layouts/",
					"~/Views/_Layouts/"
				};
				Assert.Equal(expected, paths.ToArray());
			}

			[Fact]
			public void HomeController_in_area_Admin_renamed_Layouts_folder()
			{
				var httpContext = new Mock<HttpContextBase>();
				var controller = new Mock<ControllerBase>();
				var routeData = new RouteData();
				routeData.Values.Add("controller", "Home");
				routeData.DataTokens.Add("area", "Admin");
				var controllerContext = new ControllerContext(httpContext.Object, routeData, controller.Object);

				var dpp = new DefaultPathsProvider();
				dpp.LayoutsFolderName = "Renamed";
				var paths = dpp.GetLayoutFolders(controllerContext);

				var expected = new string[] {
					"~/Areas/Admin/Views/Home/Renamed/",
					"~/Areas/Admin/Views/Renamed/",
					"~/Views/Home/Renamed/",
					"~/Views/Renamed/"
				};
				Assert.Equal(expected, paths.ToArray());
			}

			[Fact]
			public void HomeController_in_area_Admin_renamed_Areas_folder()
			{
				var httpContext = new Mock<HttpContextBase>();
				var controller = new Mock<ControllerBase>();
				var routeData = new RouteData();
				routeData.Values.Add("controller", "Home");
				routeData.DataTokens.Add("area", "Admin");
				var controllerContext = new ControllerContext(httpContext.Object, routeData, controller.Object);

				var dpp = new DefaultPathsProvider();
				dpp.AreasFolderName = "Renamed";
				var paths = dpp.GetLayoutFolders(controllerContext);

				var expected = new string[] {
					"~/Renamed/Admin/Views/Home/_Layouts/",
					"~/Renamed/Admin/Views/_Layouts/",
					"~/Views/Home/_Layouts/",
					"~/Views/_Layouts/"
				};
				Assert.Equal(expected, paths.ToArray());
			}

			[Fact]
			public void HomeController_in_area_Admin_renamed_Shared_folder()
			{
				var httpContext = new Mock<HttpContextBase>();
				var controller = new Mock<ControllerBase>();
				var routeData = new RouteData();
				routeData.Values.Add("controller", "Home");
				routeData.DataTokens.Add("area", "Admin");
				var controllerContext = new ControllerContext(httpContext.Object, routeData, controller.Object);

				var dpp = new DefaultPathsProvider();
				dpp.SharedViewsFolderName = "Renamed";
				var paths = dpp.GetLayoutFolders(controllerContext);

				var expected = new string[] {
					"~/Areas/Admin/Views/Home/_Layouts/",
					"~/Areas/Admin/Views/_Layouts/",
					"~/Views/Home/_Layouts/",
					"~/Views/_Layouts/"
				};
				Assert.Equal(expected, paths.ToArray());
			}

			[Fact]
			public void HomeController_in_area_Admin_no_Shared_folder()
			{
				var httpContext = new Mock<HttpContextBase>();
				var controller = new Mock<ControllerBase>();
				var routeData = new RouteData();
				routeData.Values.Add("controller", "Home");
				routeData.DataTokens.Add("area", "Admin");
				var controllerContext = new ControllerContext(httpContext.Object, routeData, controller.Object);

				var dpp = new DefaultPathsProvider();
				dpp.SharedViewsFolderName = null;
				var paths = dpp.GetLayoutFolders(controllerContext);

				var expected = new string[] {
					"~/Areas/Admin/Views/Home/_Layouts/",
					"~/Areas/Admin/Views/_Layouts/",
					"~/Views/Home/_Layouts/",
					"~/Views/_Layouts/"
				};
				Assert.Equal(expected, paths.ToArray());
			}
		}

		public class GetPartialsFolders
		{
			[Fact]
			public void HomeController()
			{
				var httpContext = new Mock<HttpContextBase>();
				var controller = new Mock<ControllerBase>();
				var routeData = new RouteData();
				routeData.Values.Add("controller", "Home");
				var controllerContext = new ControllerContext(httpContext.Object, routeData, controller.Object);

				var dpp = new DefaultPathsProvider();
				var paths = dpp.GetPartialsFolders(controllerContext);

				var expected = new string[] {
					"~/Views/Home/_Partials/",
					"~/Views/_Partials/"
				};
				Assert.Equal(expected, paths.ToArray());
			}

			[Fact]
			public void BlogController()
			{
				var httpContext = new Mock<HttpContextBase>();
				var controller = new Mock<ControllerBase>();
				var routeData = new RouteData();
				routeData.Values.Add("controller", "Blog");
				var controllerContext = new ControllerContext(httpContext.Object, routeData, controller.Object);

				var dpp = new DefaultPathsProvider();
				var paths = dpp.GetPartialsFolders(controllerContext);

				var expected = new string[] {
					"~/Views/Blog/_Partials/",
					"~/Views/_Partials/"
				};
				Assert.Equal(expected, paths.ToArray());
			}

			[Fact]
			public void HomeController_in_area_Admin()
			{
				var httpContext = new Mock<HttpContextBase>();
				var controller = new Mock<ControllerBase>();
				var routeData = new RouteData();
				routeData.Values.Add("controller", "Home");
				routeData.DataTokens.Add("area", "Admin");
				var controllerContext = new ControllerContext(httpContext.Object, routeData, controller.Object);

				var dpp = new DefaultPathsProvider();
				var paths = dpp.GetPartialsFolders(controllerContext);

				var expected = new string[] {
					"~/Areas/Admin/Views/Home/_Partials/",
					"~/Areas/Admin/Views/_Partials/",
					"~/Views/Home/_Partials/",
					"~/Views/_Partials/"
				};
				Assert.Equal(expected, paths.ToArray());
			}

			[Fact]
			public void HomeController_AllowControllerlessViewFolder()
			{
				var httpContext = new Mock<HttpContextBase>();
				var controller = new Mock<ControllerBase>();
				var routeData = new RouteData();
				routeData.Values.Add("controller", "Home");
				var controllerContext = new ControllerContext(httpContext.Object, routeData, controller.Object);

				var dpp = new DefaultPathsProvider();
				dpp.AllowControllerlessViewFolder = true;
				var paths = dpp.GetPartialsFolders(controllerContext);

				var expected = new string[] {
					"~/Views/Home/_Partials/",
					"~/Views/_Partials/"
				};
				Assert.Equal(expected, paths.ToArray());
			}

			[Fact]
			public void HomeController_in_area_Admin_AllowControllerlessViewFolder()
			{
				var httpContext = new Mock<HttpContextBase>();
				var controller = new Mock<ControllerBase>();
				var routeData = new RouteData();
				routeData.Values.Add("controller", "Home");
				routeData.DataTokens.Add("area", "Admin");
				var controllerContext = new ControllerContext(httpContext.Object, routeData, controller.Object);

				var dpp = new DefaultPathsProvider();
				dpp.AllowControllerlessViewFolder = true;
				var paths = dpp.GetPartialsFolders(controllerContext);

				var expected = new string[] {
					"~/Areas/Admin/Views/Home/_Partials/",
					"~/Areas/Admin/Views/_Partials/",
					"~/Views/Home/_Partials/",
					"~/Views/_Partials/"
				};
				Assert.Equal(expected, paths.ToArray());
			}

			[Fact]
			public void HomeController_in_area_Admin_renamed_Partials_folder()
			{
				var httpContext = new Mock<HttpContextBase>();
				var controller = new Mock<ControllerBase>();
				var routeData = new RouteData();
				routeData.Values.Add("controller", "Home");
				routeData.DataTokens.Add("area", "Admin");
				var controllerContext = new ControllerContext(httpContext.Object, routeData, controller.Object);

				var dpp = new DefaultPathsProvider();
				dpp.PartialsFolderName = "Renamed";
				var paths = dpp.GetPartialsFolders(controllerContext);

				var expected = new string[] {
					"~/Areas/Admin/Views/Home/Renamed/",
					"~/Areas/Admin/Views/Renamed/",
					"~/Views/Home/Renamed/",
					"~/Views/Renamed/"
				};
				Assert.Equal(expected, paths.ToArray());
			}

			[Fact]
			public void HomeController_in_area_Admin_renamed_Areas_folder()
			{
				var httpContext = new Mock<HttpContextBase>();
				var controller = new Mock<ControllerBase>();
				var routeData = new RouteData();
				routeData.Values.Add("controller", "Home");
				routeData.DataTokens.Add("area", "Admin");
				var controllerContext = new ControllerContext(httpContext.Object, routeData, controller.Object);

				var dpp = new DefaultPathsProvider();
				dpp.AreasFolderName = "Renamed";
				var paths = dpp.GetPartialsFolders(controllerContext);

				var expected = new string[] {
					"~/Renamed/Admin/Views/Home/_Partials/",
					"~/Renamed/Admin/Views/_Partials/",
					"~/Views/Home/_Partials/",
					"~/Views/_Partials/"
				};
				Assert.Equal(expected, paths.ToArray());
			}

			[Fact]
			public void HomeController_in_area_Admin_renamed_Shared_folder()
			{
				var httpContext = new Mock<HttpContextBase>();
				var controller = new Mock<ControllerBase>();
				var routeData = new RouteData();
				routeData.Values.Add("controller", "Home");
				routeData.DataTokens.Add("area", "Admin");
				var controllerContext = new ControllerContext(httpContext.Object, routeData, controller.Object);

				var dpp = new DefaultPathsProvider();
				dpp.SharedViewsFolderName = "Renamed";
				var paths = dpp.GetPartialsFolders(controllerContext);

				var expected = new string[] {
					"~/Areas/Admin/Views/Home/_Partials/",
					"~/Areas/Admin/Views/_Partials/",
					"~/Views/Home/_Partials/",
					"~/Views/_Partials/"
				};
				Assert.Equal(expected, paths.ToArray());
			}

			[Fact]
			public void HomeController_in_area_Admin_no_Shared_folder()
			{
				var httpContext = new Mock<HttpContextBase>();
				var controller = new Mock<ControllerBase>();
				var routeData = new RouteData();
				routeData.Values.Add("controller", "Home");
				routeData.DataTokens.Add("area", "Admin");
				var controllerContext = new ControllerContext(httpContext.Object, routeData, controller.Object);

				var dpp = new DefaultPathsProvider();
				dpp.SharedViewsFolderName = null;
				var paths = dpp.GetPartialsFolders(controllerContext);

				var expected = new string[] {
					"~/Areas/Admin/Views/Home/_Partials/",
					"~/Areas/Admin/Views/_Partials/",
					"~/Views/Home/_Partials/",
					"~/Views/_Partials/"
				};
				Assert.Equal(expected, paths.ToArray());
			}
		}
	}
}
