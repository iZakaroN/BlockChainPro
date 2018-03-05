using System;
using System.Collections.Generic;
using System.Linq;
using BlockChanPro.Core.Contracts;
using BlockChanPro.Core.Serialization;

namespace BlockChanPro.Core.Engine
{
	public class MinerFactory
	{
		private readonly Cryptography _cryptography;

		public MinerFactory(Cryptography cryptography)
		{
			_cryptography = cryptography;
		}
		public Miner Create(
			Address inFavor, 
			BlockHashed lastBlock, 
			IEnumerable<TransactionSigned> transactionsToProcess,
			IFeedBack feedback)
		{
			var blockToProcess = GetNewBlock(inFavor, lastBlock, transactionsToProcess);

			var targetHashBits = Rules.CalculateTargetHash(lastBlock, blockToProcess);
			return Create(inFavor, blockToProcess, targetHashBits, feedback);
		}

		public  Miner Create(
			Address sign, 
			BlockData blockToProcess, 
			HashBits targetHashBits,
			IFeedBack feedback)
		{
			var signedBlock = _cryptography.SignBlock(blockToProcess, sign, targetHashBits);

			var dataToHash = signedBlock.SerializeToBinary();
			var signedBlockHash = _cryptography.CalculateHash(dataToHash);
			return new Miner(targetHashBits, signedBlock, signedBlockHash, _cryptography, feedback);
		}

		private BlockData GetNewBlock(Address inFavor, BlockHashed lastBlock, IEnumerable<TransactionSigned> transactionsToProcess)
		{
			var transactions = GetNewBlockTransactions(inFavor, lastBlock, transactionsToProcess);
			var blockToProcess = new BlockData(
				lastBlock.Signed.Data.Index + 1,
				DateTime.UtcNow.Ticks,
				"^v^",
				transactions.ToArray(),
				lastBlock.HashTarget.Hash);
			return blockToProcess;
		}

		private IEnumerable<TransactionSigned> GetNewBlockTransactions(Address inFavor, BlockHashed lastBlock, IEnumerable<TransactionSigned> transactionsToProcess)
		{
			var mineReward = GenerateCoinbaseTransaction(inFavor, lastBlock);
			var transactions = mineReward.Union(transactionsToProcess);
			return transactions;
		}

		//TODO: Allow multiple recipients
		private IEnumerable<TransactionSigned> GenerateCoinbaseTransaction(Address inFavor, BlockHashed lastBlock)
		{
			var mineReward = new[]
			{
				_cryptography.Sign(
					new Transaction(
					Address.God,
					new[] { new Recipient(inFavor, Rules.CalulateBlockReward(lastBlock)) }, 
					0),
					inFavor),
			};
			return mineReward;
		}
	}
}
