using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HandlebarsDotNet.Mvc
{
	/// <summary>
	/// Represents the exception that is thrown for a HandlebarsDotNet.Mvc helper.
	/// </summary>
	/// <remarks>
	/// This exception is for example thrown for required arguments to the helper (like a path wasn't specified in a view file) but not for the arguments to the function
	/// that is part of the infrastructure for HandlebarsDotNet helpers (for example the TextWriter is <see langword="null"/>).
	/// </remarks>
	[Serializable]
	public class HelperException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="HelperException"/> class.
		/// </summary>
		public HelperException()
			: base()
		{
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="HelperException"/> class with a specified error message.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		public HelperException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HelperException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="innerException">The exception that is the cause of the current exception, or <see langword="null"/> if no inner exception is specified.</param>
		public HelperException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HelperException"/> class with serialized data.
		/// </summary>
		/// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
		/// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
		protected HelperException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}

// TODO: Should add a HelperArgumentRequiredException that only takes the name of the required argument (just like ArgumentNullException)
