using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandlebarsDotNet.Mvc
{
	/// <summary>
	/// A logger that does nothing. This is the default logger for <see cref="HandlebarsViewEngine"/>.
	/// </summary>
	public class NullLogger : ILogger
	{
		private static readonly Lazy<ILogger> lazy = new Lazy<ILogger>(() => new NullLogger());

		/// <summary>
		/// A singleton instance of <see cref="NullLogger"/>.
		/// </summary>
		public static ILogger Instance
		{
			get
			{
				return lazy.Value;
			}
		}

		/// <summary>
		/// The <see cref="NullLogger"/> does nothing in this <see cref="ILogger"/> implementation.
		/// </summary>
		/// <param name="category">The category the message is used for.</param>
		/// <param name="messageGenerator"></param>
		public void Log(LoggerCategory category, Func<string> messageGenerator)
		{
		}

		/// <summary>
		/// The <see cref="NullLogger"/> does nothing in this <see cref="ILogger"/> implementation.
		/// </summary>
		/// <param name="category">The category the message is used for.</param>
		/// <param name="messageGenerator"></param>
		public void Warn(LoggerCategory category, Func<string> messageGenerator)
		{
		}

		/// <summary>
		/// The <see cref="NullLogger"/> does nothing in this <see cref="ILogger"/> implementation.
		/// </summary>
		/// <param name="category">The category the message is used for.</param>
		/// <param name="messageGenerator"></param>
		public void Trace(LoggerCategory category, Func<string> messageGenerator)
		{
		}
	}
}
