namespace BlockChanPro.Core.Contracts
{
	public class TransactionSigned 
	{
		public static TransactionSigned[] Genesis =
		{
			new TransactionSigned(Transaction.Genesis, Hash.Genesis)
		};
		public Transaction Data { get; }
		public Hash Sign { get; }

		public TransactionSigned(Transaction data, Hash hash)
		{
			Data = data;
			Sign = hash;
		}
	}
}