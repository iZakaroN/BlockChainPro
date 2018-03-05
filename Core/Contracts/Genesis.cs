using System;
using BlockChanPro.Core.Engine;
using BlockChanPro.Model.Contracts;

namespace BlockChanPro.Core.Contracts
{
	public static class Genesis
	{
		public static Address God = new Address("God".Hash());
		public static Address Adam = new Address("Adam".Hash());
		public static Address Eva = new Address("Eva".Hash());
		//For easy use just use int for now

		public static long Reward = 0x100;

		public static Transaction Transaction =
			new Transaction(
				God,
				new[]
				{
					new Recipient(Adam, Reward),
					new Recipient(Eva, Reward)
				},
				0,
				0);

		public static TransactionSigned[] TransactionSigned =
		{
			new TransactionSigned(Transaction, Hash.Genesis)
		};

		public static BlockData BlockData =>
			new BlockData(
				0,
				DateTime.UtcNow.Ticks,
				"Fiat lux",
				TransactionSigned,
				Hash.Genesis);

	}
}