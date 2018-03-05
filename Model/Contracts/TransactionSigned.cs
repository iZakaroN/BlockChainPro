namespace BlockChanPro.Model.Contracts
{
	public class TransactionSigned 
	{
		public Transaction Data { get; }
		public Hash Sign { get; }

		public TransactionSigned(Transaction data, Hash hash)
		{
			Data = data;
			Sign = hash;
		}
	}
}