﻿using System;

namespace BlockChanPro.Model.Contracts
{
    //TODO: Rename to block header and move transactions into signed block
	public class BlockData
	{
		public static long BlockTime = TimeSpan.FromSeconds(5).Ticks;

		public BlockData(int index, long timeStamp, string message, TransactionSigned[] transactions, Hash previousHash)
	    {
		    Index = index;
		    TimeStamp = timeStamp;
		    Message = message;
		    Transactions = transactions;
		    PreviousHash = previousHash;
	    }

		public int Index { get; }
		public long TimeStamp { get; }

		// TODO: move message to transaction
        public string Message { get; }
		public TransactionSigned[] Transactions { get; }
		public Hash PreviousHash { get; }

	}
}
