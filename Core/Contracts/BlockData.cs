﻿using System;

namespace BlockChanPro.Core.Contracts
{
    //TODO: Rename to block header and move transactions into signed block
	public class BlockData
	{
		public static long BlockTime = TimeSpan.FromSeconds(5).Ticks;
		public static BlockData Genesis =>
			new BlockData(
				0,
				DateTime.UtcNow.Ticks,
				"Fiat lux",
				TransactionSigned.Genesis,
				Hash.Genesis);

		public BlockData()
		{
			
		}
		public BlockData(int number, long timeStamp, string message, TransactionSigned[] transactions, Hash previousHash)
	    {
		    Index = number;
		    TimeStamp = timeStamp;
		    Message = message;
		    Transactions = transactions;
		    PreviousHash = previousHash;
	    }

		public int Index { get; }
	    public long TimeStamp { get; set; }

		// TODO: move message to transaction
        public string Message { get; }
		public TransactionSigned[] Transactions { get; }
		public Hash PreviousHash { get; }

	}
}
