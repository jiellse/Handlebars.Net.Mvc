using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Caching;
using System.Web.Mvc;
using HandlebarsDotNet.Mvc.Tests.TestInternal;
using Moq;
using Xunit;

namespace HandlebarsDotNet.Mvc.Tests
{
	public class CachingTests
	{
		HandlebarsViewEngine _hbve;
		Mock<ICache> _mockCache;

		public CachingTests()
		{
			// Constructor - run before each test

			VPP vpp = new VPP(
				new VPP.Dir("Views",
					new VPP.Dir("_Layouts",
						new VPP.File("default.hbs", "Body: {{{body}}}")
						),
					new VPP.Dir("_Shared"),
					new VPP.Dir("Home",
						new VPP.File("Index.hbs", "{{!< default}} {{val}}")
						)
					)
				);

			var dumbcache = new Dictionary<string, object>();

			_mockCache = new Mock<ICache>();

			_mockCache.Setup<object>(c => c.Get(It.IsAny<string>()))
				.Returns((string key) => dumbcache.ContainsKey(key) ? dumbcache[key] : null);

			_mockCache.Setup(c => c.Insert(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CacheDependency>()))
				.Callback((string key, object val, CacheDependency cd) =>
					dumbcache[key] = val);
			_mockCache.Setup(c => c.Insert(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CacheDependency>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>()))
				.Callback((string key, object val, CacheDependency cd, DateTime dt, TimeSpan ts) =>
					dumbcache[key] = val);
			_mockCache.Setup(c => c.Insert(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CacheDependency>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>(), It.IsAny<CacheItemUpdateCallback>()))
				.Callback((string key, object val, CacheDependency cd, DateTime dt, TimeSpan ts, CacheItemUpdateCallback cb) =>
					dumbcache[key] = val);

			_hbve = new HandlebarsViewEngine();
			_hbve.VirtualPathProvider = vpp;
			_hbve.Cache = _mockCache.Object;
		}

		[Fact]
		public void Config_is_cached_once()
		{
			var controllerContext = new ControllerContext();
			controllerContext.RouteData.Values.Add("controller", "Home");

			_hbve.FindView(controllerContext, "Index", masterName: null, useCache: false);

			_mockCache.Verify(c => c.Insert(It.IsRegex(".*::Home:config$"), It.IsNotNull<IHandlebars>(), It.IsNotNull<CacheDependency>()), Times.Once);
			_mockCache.Verify(c => c.Remove(It.IsAny<string>()), Times.Never);
		}

		// TODO: Add other cache tests in this file.
	}
}
