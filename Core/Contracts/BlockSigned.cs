namespace BlockChanPro.Core.Contracts
{
	public class BlockSigned
	{
		public BlockSigned()
		{
			
		}
		public BlockSigned(BlockData block, Address stamp, TargetHashBits targetHashBits)
		{
            Data = block;
			Stamp = stamp;
            TargetHashBits = targetHashBits;
		}

		public BlockData Data { get; }
        //TODO: Change with public key and signed merkel tree root hash
		public Address Stamp { get; }
		public TargetHashBits TargetHashBits { get; }
	}
}