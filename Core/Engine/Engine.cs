using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlockChanPro.Core.Contracts;
using BlockChanPro.Core.Engine.Data;
using BlockChanPro.Core.Engine.Network;
using BlockChanPro.Model.Contracts;
using BlockChanPro.Model.Serialization;

namespace BlockChanPro.Core.Engine
{
	//TODO: Separate miner in different interface and probably class (at least as preparation for pooled mining)
	public class Engine : IEngine
	{
		private readonly IFeedback _feedback;
		private readonly IP2PNetwork _network;
		private readonly IChainData _chainData;
		private readonly ManualResetEventSlim _minerSync = new ManualResetEventSlim(true);

		//TODO: inject from factory
		private static readonly Cryptography Cryptography = new Cryptography();
		private static readonly MinerFactory MinerFactory = new MinerFactory(Cryptography);

		private Miner _currentMiner;
		// ReSharper disable once NotAccessedField.Local //Just in case
		private Task _minerTask;

		public Engine(
			IFeedback feedback,
			IP2PNetwork network,
			IChainData chainData)
		{
			_feedback = feedback;
			_network = network;
			_chainData = chainData;
		}

		/// <summary>
		/// Start mining or continue with different arguments
		/// </summary>
		/// <param name="mineAddress"></param>
		/// <param name="numberOfThreads"></param>
		/// <returns></returns>
		public void Mine(Address mineAddress, int? numberOfThreads)
		{
			if (_currentMiner != null && _currentMiner.Address.Value != mineAddress.Value)
				MineStop();

			if (_currentMiner == null)
				_minerTask = MineAsync(mineAddress, numberOfThreads);
			else
				_currentMiner.Start(numberOfThreads ?? Environment.ProcessorCount);
		}

		private async Task MineAsync(Address mineAddress, int? numberOfThreads)
		{
			if (_chainData.GetLastBlock() == null)
				await MineGenesisAsync(numberOfThreads);

			Miner miner;
			do
			{
				_minerSync.Wait();
				var lastBlock = _chainData.GetLastBlock();
				var transactions = _chainData.SelectTransactionsToMine();

				miner = await MineAsync(mineAddress, numberOfThreads, lastBlock, transactions);
				// Start next miner with same number of threads in case they was changed
				numberOfThreads = miner.Threads;

			} while (!miner.Stopped);
		}

		public async Task<Miner> MineAsync(Address mineAddress, int? numberOfThreads, BlockHashed lastBlock, IEnumerable<TransactionSigned> transactions)
		{
			var miner = MinerFactory.Create(mineAddress, lastBlock, transactions, _feedback);
			var threadsClosure = numberOfThreads;

			await _feedback.Execute("Mine",
				() => MineAsync(miner, threadsClosure),
				() => $"{nameof(mineAddress)}: {mineAddress}, {nameof(numberOfThreads)}: {threadsClosure}");
			return miner;
		}

		public void MineStop()
		{
			if (_currentMiner != null)
			{
				_currentMiner.Stop();
				_currentMiner = null;
			}
		}

		public async Task<BlockHashed> MineGenesisAsync(int? numberOfThreads)
		{
			var genesisMiner = MinerFactory.Create(Genesis.GetBlockData(Cryptography, DateTime.UtcNow.Ticks), _feedback);
			return await _feedback.Execute("MineGenesis",
				() => MineAsync(genesisMiner, numberOfThreads),
				() => $"{nameof(numberOfThreads)}: {numberOfThreads}");
		}

		private Task<BlockHashed> MineAsync(Miner miner, int? numberOfThreads)
		{
			miner.Start(numberOfThreads ?? Environment.ProcessorCount);
			_feedback.MineNewBlock(miner.Difficulty, miner.TargetBits);
			_currentMiner = miner;
			return AddMinedBlockAsync(miner);
		}

		private async Task<BlockHashed> AddMinedBlockAsync(Miner miner)
		{
			return await _feedback.Execute("AddMinedBlockAsync",
				async () =>
				{
					var minedBlock = await miner.GetBlock();
					if (minedBlock != null)
					{
						await _network.BroadcastAsync(minedBlock);

						_feedback.NewBlockMined(minedBlock.Signed.Data.Index, DateTime.UtcNow.Ticks - minedBlock.Signed.Data.TimeStamp);
						AddNewBlock(minedBlock);

						_currentMiner = null;
						return minedBlock;
					}
					_feedback.MineCanceled();
					return null;
				});
		}

		private void AddNewBlock(BlockHashed newBlock)
		{
			_feedback.Execute("AddNewBlock",
				() =>
				{
					//TODO: Any chance the block to need sync here? Rely on AcceptBlockAsync for now
					_chainData.AddNewBlock(newBlock);
				},
				() => $"{nameof(newBlock)}: {newBlock.SerializeToJson()}");
		}

		public void SendTransaction(TransactionSigned result)
		{
			if (_chainData.AddPendingTransaction(result))
				_network.BroadcastAsync(new[] { result });
		}

		public Task AcceptTransactionsAsync(TransactionsBundle transactions)
		{
			foreach (var transaction in transactions.Transactions)
				_chainData.AddPendingTransaction(transaction);
			return Task.CompletedTask;
		}

		public Task AcceptBlockAsync(BlockBundle block)
		{
			if (block?.Block != null)
			{
				_minerSync.Reset();
				var blockchainState = _chainData.AddNewBlock(block.Block);
				//Cancel miner only after new block was accepted
				_currentMiner?.Cancel();
				if (blockchainState == BlockchainState.NeedSync)
					StartBlockchainSync(() => _minerSync.Set());
				else
					_minerSync.Set();
				return Task.CompletedTask;
			}
			throw new BlockchainException("Cannot accept NULL block");
		}

		private Task _blockchainSync;
		private void StartBlockchainSync(Action syncCompletedAction)
		{
			if (_blockchainSync == null)
				_blockchainSync = BlockchainSync(syncCompletedAction);
		}

		public void StartBlockchainSync()
		{
			StartBlockchainSync(() => {});
		}
		private async Task BlockchainSync(Action syncCompletedAction)
		{
			try
			{
				await _network.BlockchainSync(_chainData);
				syncCompletedAction();
			}
			catch (Exception e)
			{
				_feedback.Error(nameof(BlockchainSync), e.Message);
			}
			_blockchainSync = null;
		}

		public Task<int> ConnectToPeerAsync(string url)
		{
			return _network.ConnectToPeerAsync(url);
		}
	}
}
