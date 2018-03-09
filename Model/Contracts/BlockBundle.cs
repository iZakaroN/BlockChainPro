namespace BlockChanPro.Model.Contracts
{
    public class BlockBundle
    {
	    public BlockHashed Block { get; }
	    public string Sender { get; }

	    public BlockBundle(BlockHashed block, string sender)
	    {
		    Block = block;
		    Sender = sender;
	    }
	}
}
