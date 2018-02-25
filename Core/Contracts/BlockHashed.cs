namespace BlockChanPro.Core.Contracts
{
    public class BlockHashed
	{
		public BlockHashed(BlockSigned block, HashTarget hashTarget)
		{
            Signed = block;
			HashTarget = hashTarget;
		}

		public BlockSigned Signed { get; }
	    public HashTarget HashTarget { get; }
	}
}