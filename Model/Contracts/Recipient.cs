namespace BlockChanPro.Model.Contracts
{
	public class Recipient
	{
		public Recipient(Address address, long amount)
		{
			Address = address;
			Amount = amount;
		}

		public Address Address { get; }
		public long Amount { get; }
	}
}