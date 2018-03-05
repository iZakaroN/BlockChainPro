namespace BlockChanPro.Model.Contracts
{
	public struct Address
	{
		public Address(Hash value)
		{
			Value = value;
		}

		public Hash Value { get; }

	}
}