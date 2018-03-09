using System;

namespace BlockChanPro.Model.Contracts
{
	//TODO: Introduce txin instead of sender and fee
	public class Transaction
	{
		public static int BlockCountRewardReduction = 0x100;

		public Transaction(Address sender, Recipient[] recipients, decimal fee, long? timeStamp = null)
		{
			Sender = sender;
			Recipients = recipients;
			Fee = fee;
			TimeStamp = timeStamp ?? DateTime.UtcNow.Ticks;
		}

		public Address Sender { get; }
		public Recipient[] Recipients { get; }
		public decimal Fee { get; }
		public long TimeStamp { get; }
	}
}