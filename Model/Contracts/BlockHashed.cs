namespace BlockChanPro.Model.Contracts
{
    public class BlockHashed
	{
		public BlockHashed(BlockSigned signed, HashTarget hashTarget)
		{
            Signed = signed;
			HashTarget = hashTarget;
		}

		public BlockSigned Signed { get; }
	    public HashTarget HashTarget { get; }
	}
}