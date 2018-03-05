namespace BlockChanPro.Core.Engine.Data
{

	public class TransactionsInfo
	{
		public TransactionsInfo(long confirmedTransactions, long pendingTransactions)
		{
			ConfirmedTransactions = confirmedTransactions;
			PendingTransactions = pendingTransactions;
		}
		public long ConfirmedTransactions { get; }
		public long PendingTransactions { get; }
	}
}
