using System;

namespace BlockChanPro.Core.Contracts
{
	public class Transaction
	{
		public static long GenesisReward = 0x100;
		public static int BlockCountRewardReduction = 0x100;
		public static Transaction[] Genesis =
		{
			new Transaction(
				Address.God, 
				new [] {
					new Recipient(Address.Adam, GenesisReward),
					new Recipient(Address.Eva,  GenesisReward) },
				0,
				0)
		};

		public Transaction()
		{
			
		}
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