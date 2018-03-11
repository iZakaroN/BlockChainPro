namespace BlockChanPro.Model.Contracts
{
    public static class Extensions
    {
	    public static BlockIdentity Identity(this BlockHashed block)
	    {
		    return block == null ? null : new BlockIdentity(block.Signed.Data.Index, block.HashTarget.Hash);
	    }
	}
}
