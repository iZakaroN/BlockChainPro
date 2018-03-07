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
			new TransactionSigned(Transaction, Hash)
		};

		public static BlockSigned GetBlockData(Cryptography cryptography, long timeStamp)
		{
			var blockData = new BlockData(
				0,
				timeStamp,
				"Fiat lux",
				TransactionSigned,
				Hash);
			var signedBlock = cryptography.SignBlock(blockData, God, Target);
			return signedBlock;
		}

		public static readonly HashBits Target = new HashBits(0x0f, 0xffffffffffffff);

		public static Hash Hash => new Hash(new byte[]
		{
			0,0,0,0,0,0,0,0,
			0,0,0,0,0,0,0,0,
			0,0,0,0,0,0,0,0,
			0,0,0,0,0,0,0,0,
		});

		public const int AdjustmentPercentLimit = 20;

		/*public static Hash GenesisTargetHash = new Hash(new byte[]
        {
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
        });*/


	}
}