using System;
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
			var blockTime = newBlock.Signed.Data.TimeStamp - lastBlock?.Signed.Data.TimeStamp ?? 0;
			try
			{
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
				_feedback.NewBlockAccepted(newBlock.Signed.Data.Index, blockTime, newBlock.HashTarget.Hash);
			}
			catch (Exception e)
			{
				_feedback.NewBlockRejected(newBlock.Signed.Data.Index, blockTime, newBlock.HashTarget.Hash, e.Message);
			}

		}

		public void ValidateGenesisBlock(BlockHashed newBlock)
		{
			var expectedSignedGenesis = Genesis.GetBlockData(_cryptography, newBlock.Signed.Data.TimeStamp);
			ValidateBlockHash(expectedSignedGenesis, newBlock.HashTarget);
		}

		public void ValidateBlock(BlockHashed lastBlock, BlockHashed newBlock)
		{
			ValidateParent(lastBlock, newBlock);
			ValidateHashTarget(lastBlock, newBlock);
			ValidateBlockHash(newBlock);
			ValidateTransactions(newBlock.Signed.Data.Transactions);
		}

		private static void ValidateParent(BlockHashed lastBlock, BlockHashed newBlock)
		{
			if (newBlock.Signed.Data.Index != lastBlock.Signed.Data.Index + 1)
				throw new BlockchainException("Not sequential block"); //TODO: try re-sync
			if (newBlock.Signed.Data.ParentHash != lastBlock.HashTarget.Hash)
				throw new BlockchainException("Parent hash do not match"); //TODO: try re-sync
		}

		public void ValidateHashTarget(BlockHashed lastBlock, BlockHashed newBlock)
		{
			var targetHashBits = Rules.CalculateTargetHash(lastBlock, newBlock.Signed.Data);
			if (newBlock.Signed.HashTargetBits.Value != targetHashBits.Value)
				throw new BlockchainException("Block target hash bits are not valid");
			var targetHash = newBlock.Signed.HashTargetBits.ToHash();
			if (newBlock.HashTarget.Hash.Compare(targetHash) >= 0)
				throw new BlockchainException("Block hash is not below a necessary target");
		}

		public void ValidateBlockHash(BlockHashed newBlock)
		{
			ValidateBlockHash(newBlock.Signed, newBlock.HashTarget);
		}

		public void ValidateBlockHash(BlockSigned blockSigned, HashTarget target)
		{
			var calulatedSignedHash = _cryptography.CalculateHash(blockSigned);
			var calulatedSignedHashBytes = calulatedSignedHash.ToBinary();
			var calulatedHash = _cryptography.CalculateHash(calulatedSignedHashBytes, target.Nounce);
			if (calulatedHash != target.Hash)
				throw new BlockchainException("Block has invalid hash");
		}

		public void ValidateTransactions(TransactionSigned[] transactions)
		{
			//TODO:
			foreach (var transaction in transactions)
				ValidateTransaction(transaction);
		}

		public void ValidateTransaction(TransactionSigned transaction)
		{
			//TODO: Check signature by public key
			if (transaction.Sign == null)
				throw new BlockchainException("Transaction is not valid");
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
