namespace BlockChanPro.Model.Contracts
{
	public class BlockSigned
	{
		public BlockSigned(BlockData data, Address stamp, HashBits hashTargetBits)
		{
            Data = data;
			Stamp = stamp;
            HashTargetBits = hashTargetBits;
		}

		public BlockData Data { get; }
        //TODO: Change with public key and signed merkle tree root hash
		public Address Stamp { get; }
		public HashBits HashTargetBits { get; }
	}
}