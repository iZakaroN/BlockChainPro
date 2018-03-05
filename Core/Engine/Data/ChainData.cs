using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BlockChanPro.Model.Contracts;

namespace BlockChanPro.Core.Engine.Data
{
	public interface IChainData
	{
		BlockHashed GetLastBlock();

		void RemovePendingTransactions(TransactionSigned[] transactions);

		void AddNewBlock(BlockHashed newBlock);

		bool AddPendingTransaction(TransactionSigned transaction);
		IEnumerable<TransactionSigned> SelectTransactionsToMine();

		TransactionsInfo CalulateTransactionsInfo();
	}
    public class ChainData : IChainData
	{
		private List<BlockHashed> Chain { get; }
		//TODO: Order transactions in transaction.Sender->transaction.Id
		private ConcurrentDictionary<Hash, TransactionSigned> PendingTransactions { get; }

		public ChainData()
		{
			Chain = new List<BlockHashed>();
			PendingTransactions = new ConcurrentDictionary<Hash, TransactionSigned>();
		}

		public ChainData(List<BlockHashed> chain, ConcurrentDictionary<Hash, TransactionSigned> pendingTransactions)
		{
			Chain = chain;
			PendingTransactions = pendingTransactions;
		}

		public BlockHashed GetLastBlock()
		{
			if (Chain.Count>0)
				return Chain[Chain.Count - 1];
			return null;
		}

		public void RemovePendingTransactions(TransactionSigned[] transactions)
		{
			foreach (var blockTransactions in transactions)
				PendingTransactions.TryRemove(blockTransactions.Sign, out var _);
		}

		public void AddNewBlock(BlockHashed newBlock)
		{
			ValidateBlock(newBlock);
			Chain.Add(newBlock);

		}

		private void ValidateBlock(BlockHashed newBlock)
		{
			//TODO:
			//throw new NotImplementedException();
		}

		public bool AddPendingTransaction(TransactionSigned transaction)
		{
			return PendingTransactions.TryAdd(transaction.Sign, transaction);
		}

		public IEnumerable<TransactionSigned> SelectTransactionsToMine()
		{
			//Just choose add all transactions
			return PendingTransactions.Values;
		}

		public TransactionsInfo CalulateTransactionsInfo()
		{
			return new TransactionsInfo(
				Chain.Aggregate(0, (seed, block) => seed + block.Signed.Data.Transactions.Length),
				PendingTransactions.Count
			);
		}

	}
}
