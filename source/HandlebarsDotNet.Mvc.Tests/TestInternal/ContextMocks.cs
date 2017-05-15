using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Moq;

// http://code.google.com/p/northwinddemo/source/browse/trunk/Source/Tests/Northwinddemo.Web.Tests/ContextMocks.cs

// http://blog.kurtschindler.net/extending-steve-sandersons-contextmocks-class/

// Some changes by Jiell

namespace HandlebarsDotNet.Mvc.Tests.TestInternal
{
	public class ContextMocks
	{
		public ContextMocks()
		{
			HttpContext  = new Mock<HttpContextBase>();
			Request      = new Mock<HttpRequestBase>();
			Response     = new Mock<HttpResponseBase>();
			Cache        = new Mock<HttpCachePolicyBase>();
			OutputStream = new Mock<Stream>();

			HttpContext.Setup(x => x.Request).Returns(Request.Object);
			HttpContext.Setup(x => x.Response).Returns(Response.Object);
			HttpContext.Setup(x => x.Session).Returns(new FakeSessionState());

			Request.SetupGet(r => r.Headers).Returns(new NameValueCollection());
			Request.Setup(r => r.Cookies).Returns(new HttpCookieCollection());
			Request.Setup(r => r.QueryString).Returns(new NameValueCollection());
			Request.Setup(r => r.Form).Returns(new NameValueCollection());
			this.SetHttpMethod("GET");

			Response.SetupGet(r => r.Headers).Returns(new NameValueCollection());
			Response.Setup(r => r.Cookies).Returns(new HttpCookieCollection());
			Response.Setup(r => r.Cache).Returns(Cache.Object);
			Response.Setup(r => r.OutputStream).Returns(OutputStream.Object);
			Response.SetupProperty(r => r.StatusCode, 200);
			Response.SetupProperty(r => r.ContentType);
			Response.SetupProperty(r => r.CacheControl, "private");
		}

		public ContextMocks(Controller onController)
			: this()
		{
			RequestContext rc = new RequestContext(HttpContext.Object, new RouteData());
			onController.ControllerContext = new ControllerContext(rc, onController);
		}

		public void SetHttpMethod(string httpMethod)
		{
			Request.Setup(r => r.HttpMethod).Returns(httpMethod);
		}

		public Mock<HttpContextBase> HttpContext
		{
			get;
			private set;
		}

		public Mock<HttpRequestBase> Request
		{
			get;
			private set;
		}

		public Mock<HttpResponseBase> Response
		{
			get;
			private set;
		}

		public Mock<HttpCachePolicyBase> Cache
		{
			get;
			private set;
		}

		public Mock<Stream> OutputStream
		{
			get;
			private set;
		}

		//public RouteData RouteData
		//{
		//	get;
		//	private set;
		//}

		public void SetupContextUser(string userName)
		{
			HttpContext.Setup(x => x.User).Returns(new FakePrincipal(new FakeIdentity(userName)));
		}

		private class FakeSessionState : HttpSessionStateBase
		{
			Dictionary<string, object> items = new Dictionary<string, object>();

			public override object this[string name]
			{
				get
				{
					return items.ContainsKey(name) ? items[name] : null;
				}
				set
				{
					items[name] = value;
				}
			}
		}

		private class FakePrincipal : IPrincipal
		{
			private FakeIdentity _ident;
			public FakePrincipal(FakeIdentity ident)
			{
				_ident = ident;
			}

			public IIdentity Identity
			{
				get { return _ident; }
			}

			public bool IsInRole(string role)
			{
				throw new NotImplementedException();
			}
		}

		private class FakeIdentity : IIdentity
		{
			private string _name;
			public FakeIdentity(string Name)
			{
				_name = Name;
			}

			public string AuthenticationType
			{
				get { throw new NotImplementedException(); }
			}

			public bool IsAuthenticated
			{
				get { throw new NotImplementedException(); }
			}

			public string Name
			{
				get { return _name; }
			}
		}
	}
}
