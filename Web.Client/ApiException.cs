using System;

namespace Web.Shared
{
	public class ApiException : Exception
	{
		public ApiException(string message) :
			base(message)
		{
		}
	}
}