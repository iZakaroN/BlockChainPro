using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BlockChanPro.Core.Contracts;
using BlockChanPro.Model.Contracts;
using BlockChanPro.Model.Serialization;

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
		private readonly IFeedBack _feedback;
		private readonly Cryptography _cryptography;

		private List<BlockHashed> Chain { get; }
		//TODO: Order transactions in transaction.Sender->transaction.Id
		private ConcurrentDictionary<Hash, TransactionSigned> PendingTransactions { get; }

		public ChainData(
			IFeedBack feedback,
			Cryptography cryptography)
		{
			_feedback = feedback;
			_cryptography = cryptography;
			Chain = new List<BlockHashed>();
			PendingTransactions = new ConcurrentDictionary<Hash, TransactionSigned>();
		}

		public BlockHashed GetLastBlock()
		{
			if (Chain.Count > 0)
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
			var lastBlock = GetLastBlock();
			if (lastBlock == null)
			{
				ValidateGenesisBlock(newBlock);
			}
			else
			{
				ValidateBlock(lastBlock, newBlock);
				RemovePendingTransactions(newBlock.Signed.Data.Transactions);
			}

			Chain.Add(newBlock);

		}

		private void ValidateGenesisBlock(BlockHashed newBlock)
		{
			var expectedSignedGenesis = Genesis.GetBlockData(_cryptography, newBlock.Signed.Data.TimeStamp);
			ValidateBlockHash(expectedSignedGenesis, newBlock.HashTarget);
			_feedback.NewBlockAccepted(newBlock.Signed.Data.Index, 0, newBlock.HashTarget.Hash);
		}

		private void ValidateBlock(BlockHashed lastBlock, BlockHashed newBlock)
		{
			if (newBlock.Signed.Data.Index != lastBlock.Signed.Data.Index + 1)
				throw new BlockchainException("Not sequential block");//TODO: try re-sync

			ValidateHashTarget(lastBlock, newBlock);
			ValidateBlockHash(newBlock);
			ValidateBlockTransactions(newBlock);

			var blockTime = newBlock.Signed.Data.TimeStamp - lastBlock.Signed.Data.TimeStamp;
			_feedback.NewBlockAccepted(newBlock.Signed.Data.Index, blockTime, newBlock.HashTarget.Hash);
		}

		private void ValidateHashTarget(BlockHashed lastBlock, BlockHashed newBlock)
		{
			var targetHashBits = Rules.CalculateTargetHash(lastBlock, newBlock.Signed.Data);
			if (newBlock.Signed.HashTargetBits.Value != targetHashBits.Value)
				throw new BlockchainException("Block target hash bits are not valid");
			var targetHash = newBlock.Signed.HashTargetBits.ToHash();
			if (newBlock.HashTarget.Hash.Compare(targetHash) >= 0)
				throw new BlockchainException("Block hash is not below a necessary target");
		}

		private void ValidateBlockHash(BlockHashed newBlock)
		{
			ValidateBlockHash(newBlock.Signed, newBlock.HashTarget);
		}

		private void ValidateBlockHash(BlockSigned blockSigned, HashTarget target)
		{
			var calulatedSignedHash = _cryptography.CalculateHash(blockSigned);
			var calulatedSignedHashBytes = calulatedSignedHash.ToBinary();
			var calulatedHash = _cryptography.CalculateHash(calulatedSignedHashBytes, target.Nounce);
			if (calulatedHash != target.Hash)
				throw new BlockchainException("Block has invalid hash");
		}

		private void ValidateBlockTransactions(BlockHashed newBlock)
		{
			//TODO:
		}

		public bool AddPendingTransaction(TransactionSigned transaction)
		{
			if (PendingTransactions.TryAdd(transaction.Sign, transaction))
			{
				_feedback.NewTransaction(transaction);
				return true;
			}

			return false;
		}

		public IEnumerable<TransactionSigned> SelectTransactionsToMine()
		{
			//Just add all transactions
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
