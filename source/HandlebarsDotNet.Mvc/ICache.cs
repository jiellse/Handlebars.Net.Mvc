using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Caching;

namespace HandlebarsDotNet.Mvc
{
	// This interface is used instead of using the cache directly.
	// By mocking this it is easy to verify which cache calls were made during unit tests.

	internal interface ICache
	{
		object Get(string cachekey);
		void Insert(string key, object value, CacheDependency dependencies);
		void Insert(string key, object value, CacheDependency dependencies, DateTime absoluteExpiration, TimeSpan slidingExpiration);
		void Insert(string key, object value, CacheDependency dependencies, DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemUpdateCallback onUpdateCallback);
		void Remove(string key);

		DateTime NoAbsoluteExpiration
		{
			get;
		}

		TimeSpan NoSlidingExpiration
		{
			get;
		}
	}
}
