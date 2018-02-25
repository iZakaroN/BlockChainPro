using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Serialization;

namespace Core.Contracts
{
	public class BlockData
	{
		public static long BlockTime = TimeSpan.FromSeconds(5).Ticks;
		public static BlockData Genesis =
			new BlockData(
				0,
				DateTime.UtcNow.Ticks,
				"Fiat lux",
				Transaction.Genesis,
				Hash.Genesis);

		public BlockData()
		{
			
		}
		public BlockData(int number, long timeStamp, string message, Transaction[] transactions, Hash previousHash)
	    {
		    Index = number;
		    TimeStamp = timeStamp;
		    Message = message;
		    Transactions = transactions;
		    PreviousHash = previousHash;
	    }

		public int Index { get; }
	    public long TimeStamp { get; }
	    public string Message { get; }
		public Transaction[] Transactions { get; }
		public Hash PreviousHash { get; }

	}
}
