namespace BlockChanPro.Core.Contracts
{
	public class BlockSigned
	{
		public BlockSigned(BlockData block, Address stamp, HashBits hashTargetBits)
		{
            Data = block;
			Stamp = stamp;
            HashTargetBits = hashTargetBits;
		}

		public BlockData Data { get; }
        //TODO: Change with public key and signed merkle tree root hash
		public Address Stamp { get; }
		public HashBits HashTargetBits { get; }
	}
}