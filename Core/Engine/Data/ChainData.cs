using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockChanPro.Core.Contracts;
using BlockChanPro.Model.Contracts;
using BlockChanPro.Model.Serialization;

namespace BlockChanPro.Core.Engine.Data
{
	public interface IChainData
	{
		BlockHashed GetLastBlock();

		void RemovePendingTransactions(TransactionSigned[] transactions);

		BlockchainState AddNewBlock(BlockHashed newBlock);

		bool AddPendingTransaction(TransactionSigned transaction);

		IEnumerable<TransactionSigned> SelectTransactionsToMine();

		TransactionsInfo CalulateTransactionsInfo();

		Task<BlockHashed[]> GetBlocksAsync(int startBlockIndex, int blocksCount);
		Task<BlockIdentity[]> GetBlockIdentitiesAsync(int startBlockIndex, int i);
		Task<BlockHashed[]> GetBlocksAsync(int[] indexes);
		Task<BlockIdentity[]> GetBlockIdentitiesAsync(int[] indexes);
	}

	public class ChainData : IChainData
	{
		private readonly IFeedback _feedback;
		private readonly Cryptography _cryptography;

		private List<BlockHashed> Chain { get; }
		//TODO: Order transactions in transaction.Sender->transaction.Id
		private ConcurrentDictionary<Hash, TransactionSigned> PendingTransactions { get; }

		public ChainData(
			IFeedback feedback,
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

		public BlockchainState AddNewBlock(BlockHashed newBlock)
		{
			var lastBlock = GetLastBlock();
			var blockTime = newBlock.Signed.Data.TimeStamp - lastBlock?.Signed.Data.TimeStamp ?? 0;
			try
			{
				if (ValidateBlock(newBlock, lastBlock, out var blockchainState))
				{

					Chain.Add(newBlock);
					_feedback.NewBlockAccepted(newBlock.Signed.Data.Index, blockTime, newBlock.HashTarget.Hash);
				}

				return blockchainState;
			}
			catch (BlockchainValidationException e)
			{
				_feedback.NewBlockRejected(newBlock.Signed.Data.Index, blockTime, newBlock.HashTarget.Hash, e.Message);
			}

			return BlockchainState.ValidationError;
		}

		private bool ValidateBlock(BlockHashed newBlock, BlockHashed lastBlock, out BlockchainState blockchainState)
		{
			bool result;
			if (lastBlock == null)
			{
				result = ValidateGenesisBlock(newBlock, out blockchainState);
			}
			else
			{
				result = ValidateNewBlock(lastBlock, newBlock, out blockchainState);
				if (result)
					RemovePendingTransactions(newBlock.Signed.Data.Transactions);
			}

			return result;
		}

		public bool ValidateGenesisBlock(BlockHashed newBlock, out BlockchainState blockchainState)
		{
			var expectedSignedGenesis = Genesis.GetBlockData(_cryptography, newBlock.Signed.Data.TimeStamp);
			var result = ValidateParent(0, Genesis.Hash, newBlock.Signed.Data,
				out blockchainState);
			if (result)
			{
				ValidateSignature(expectedSignedGenesis.Stamp, newBlock.Signed);
				ValidateBlockHash(expectedSignedGenesis, newBlock.HashTarget);
			}

			return result;
		}

		public bool ValidateNewBlock(BlockHashed lastBlock, BlockHashed newBlock, out BlockchainState blockchainState)
		{
			var result = ValidateParent(lastBlock.Signed.Data.Index + 1, lastBlock.HashTarget.Hash, newBlock.Signed.Data,
				out blockchainState);
			if (result)
			{
				ValidateHashTarget(lastBlock, newBlock);
				ValidateSignature(newBlock.Signed.Stamp, newBlock.Signed);
				ValidateBlockHash(newBlock);
				ValidateTransactions(newBlock.Signed.Data.Transactions);
			}

			return result;
		}

		private static bool ValidateParent(int expectedHeight, Hash expectedHash, BlockData newBlock, out BlockchainState blockchainState)
		{
			if (newBlock.Index > expectedHeight)
			{
				blockchainState = BlockchainState.NeedSync;
				return false;
			}

			if (newBlock.Index != expectedHeight)
				throw new BlockchainValidationException($"New block Height {newBlock.Index} do not match the expected Height {expectedHeight}");
			if (newBlock.ParentHash != expectedHash)
				throw new BlockchainValidationException($"New block parent hash {newBlock.ParentHash} do not match the expected {expectedHash}");
			blockchainState = BlockchainState.Healty;
			return true;
		}

		private void ValidateSignature(Address stamp, BlockSigned newBlockSigned)
		{
			// TODO: PubKey sign check. Temporary to validate genesis block
			if (stamp.Value != newBlockSigned.Stamp.Value)
				throw new BlockchainValidationException("Block has invalid signature");
		}

		public void ValidateHashTarget(BlockHashed lastBlock, BlockHashed newBlock)
		{
			var targetHashBits = Rules.CalculateTargetHash(lastBlock, newBlock.Signed.Data);
			if (newBlock.Signed.HashTargetBits.Value != targetHashBits.Value)
				throw new BlockchainValidationException("Block target hash bits are not valid");
			var targetHash = newBlock.Signed.HashTargetBits.ToHash();
			if (newBlock.HashTarget.Hash.Compare(targetHash) >= 0)
				throw new BlockchainValidationException("Block hash is not below a necessary target");
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
				throw new BlockchainValidationException("Block has invalid hash");
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
				throw new BlockchainValidationException("Transaction is not valid");
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

		public Task<BlockHashed[]> GetBlocksAsync(int startBlockIndex, int blocksCount)
		{
			if (Chain.Count > 0)
			{
				if (startBlockIndex == -1)
					startBlockIndex = Chain.Count - 1;
				blocksCount = Math.Min(blocksCount, Chain.Count - startBlockIndex);
				if (blocksCount > 0)
					return Task.FromResult(
						Chain.GetRange(startBlockIndex, blocksCount).ToArray());
			}
			return Task.FromResult(new BlockHashed[] { });
		}

		public Task<BlockHashed[]> GetBlocksAsync(int[] indexes)
		{
			return Task.FromResult(
				indexes
					.Where(i => i < Chain.Count)
					.Select(i => Chain[i])
					.ToArray());
		}

		public Task<BlockIdentity[]> GetBlockIdentitiesAsync(int startBlockIndex, int blocksCount)
		{
			if (Chain.Count > 0)
			{
				if (startBlockIndex == -1)
					startBlockIndex = Chain.Count - 1;
				blocksCount = Math.Min(blocksCount, Chain.Count - startBlockIndex);
				if (blocksCount > 0)
					return Task.FromResult(
						Chain
							.GetRange(startBlockIndex, blocksCount)
							.Select(b => new BlockIdentity(b.Signed.Data.Index, b.HashTarget.Hash))
							.ToArray());
			}
			return Task.FromResult(new BlockIdentity[] { });
		}

		public Task<BlockIdentity[]> GetBlockIdentitiesAsync(int[] indexes)
		{
			return Task.FromResult(
				indexes
					.Where(i => i < Chain.Count)
					.Select(i => new BlockIdentity(Chain[i].Signed.Data.Index, Chain[i].HashTarget.Hash))
					.ToArray());
		}
	}
}
