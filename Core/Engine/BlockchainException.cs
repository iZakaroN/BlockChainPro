using System;

namespace BlockChanPro.Core.Engine
{
	public class BlockchainException : Exception
	{
		public BlockchainException(string message) :
			base(message)
		{

		}
	}
}
