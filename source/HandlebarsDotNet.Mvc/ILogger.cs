using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandlebarsDotNet.Mvc
{
	/// <summary>
	/// The interface used by <see cref="HandlebarsViewEngine"/> for logging purposes.
	/// </summary>
	/// <remarks>
	/// Having a "Func&lt;string&gt; messageGenerator" argument may look strange, but that is a performance optimization:<br />
	/// If the message isn't used (maybe the <see cref="NullLogger"/> is used for example), we don't have to pay the CPU cycles for generating the formatted string to be logged.
	/// </remarks>
	public interface ILogger
	{
		/// <summary>
		/// Logs a message.
		/// </summary>
		/// <param name="category">The category the message is used for.</param>
		/// <param name="messageGenerator"></param>
		void Log(LoggerCategory category, Func<string> messageGenerator);

		/// <summary>
		/// Logs a warning message.
		/// </summary>
		/// <param name="category">The category the message is used for.</param>
		/// <param name="messageGenerator"></param>
		void Warn(LoggerCategory category, Func<string> messageGenerator);

		/// <summary>
		/// Logs a trace message.
		/// </summary>
		/// <param name="category">The category the message is used for.</param>
		/// <param name="messageGenerator"></param>
		void Trace(LoggerCategory category, Func<string> messageGenerator);
	}
}
