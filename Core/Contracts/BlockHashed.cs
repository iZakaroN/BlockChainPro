namespace BlockChanPro.Core.Contracts
{
	public class BlockHashed
	{
		public BlockHashed()
		{
			
		}
		public BlockHashed(BlockSigned block, Hash nounce, Hash hash)
		{
            Signed = block;
			Nounce = nounce;
			Hash = hash;
		}

		public BlockSigned Signed { get; }
		public Hash Nounce { get; }
		public Hash Hash { get; }
	}
}