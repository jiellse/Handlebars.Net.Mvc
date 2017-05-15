using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// This is copied from HandlebarsDotNet - https://github.com/rexm/Handlebars.Net/blob/master/source/Handlebars/EnumerableExtensions.cs

namespace HandlebarsDotNet.Mvc
{
	internal static class EnumerableExtensions
	{
#if false
		// Not using this at the moment.
		public static bool IsOneOf<TSource, TExpected>(this IEnumerable<TSource> source)
			where TExpected : TSource
		{
			var enumerator = source.GetEnumerator();
			enumerator.MoveNext();
			return (enumerator.Current is TExpected) && (enumerator.MoveNext() == false);
		}
#endif

		public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
		{
			if(dictionary.ContainsKey(key))
			{
				dictionary[key] = value;
			}
			else
			{
				dictionary.Add(key, value);
			}
		}
	}
}
