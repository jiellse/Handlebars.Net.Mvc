using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;

namespace HandlebarsDotNet.Mvc
{
	// Just forwards the calls to HttpRuntime.Cache (an instance of the sealed System.Web.Caching.Cache)

	internal class DefaultCache : ICache
	{
		public object Get(string cachekey)
		{
			return HttpRuntime.Cache.Get(cachekey);
		}

		public void Insert(string key, object value, CacheDependency dependencies)
		{
			HttpRuntime.Cache.Insert(key, value, dependencies);
		}

		public void Insert(string key, object value, CacheDependency dependencies, DateTime absoluteExpiration, TimeSpan slidingExpiration)
		{
			HttpRuntime.Cache.Insert(key, value, dependencies, absoluteExpiration, slidingExpiration);
		}

		public void Insert(string key, object value, CacheDependency dependencies, DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemUpdateCallback onUpdateCallback)
		{
			HttpRuntime.Cache.Insert(key, value, dependencies, absoluteExpiration, slidingExpiration, onUpdateCallback);
		}

		public void Remove(string key)
		{
			HttpRuntime.Cache.Remove(key);
		}

		public DateTime NoAbsoluteExpiration
		{
			get { return System.Web.Caching.Cache.NoAbsoluteExpiration; }
		}

		public TimeSpan NoSlidingExpiration
		{
			get { return System.Web.Caching.Cache.NoSlidingExpiration; }
		}
	}
}
