using System;

namespace BlockChanPro.Core.Engine
{
	public class BlockchainValidationException : BlockchainException
	{
		public BlockchainValidationException(string message) :
			base(message)
		{

		}
	}

	public class BlockchainException : Exception
	{
		public BlockchainException(string message) :
			base(message)
		{

		}
	}

}
