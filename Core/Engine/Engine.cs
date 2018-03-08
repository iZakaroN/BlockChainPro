using System;
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
	    private readonly IFeedBack _feedback;
		private readonly IP2PNetwork _network;
		private readonly IChainData _chainData;

		//TODO: inject from factory
		private static readonly Cryptography Cryptography = new Cryptography();
		private static readonly MinerFactory MinerFactory = new MinerFactory(Cryptography);

	    private Miner _currentMiner;
	    // ReSharper disable once NotAccessedField.Local //Just in case
	    private Task _minerTask;

		public Engine(
			IFeedBack feedback, 
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
		    await MineGenesisAsync(numberOfThreads);

		    do
		    {
			    var lastBlock = _chainData.GetLastBlock();
			    var miner = MinerFactory.Create(mineAddress, lastBlock, _chainData.SelectTransactionsToMine(), _feedback);
			    var threadsClosure = numberOfThreads;

				await _feedback.Execute("Mine",
				    () => MineAsync(miner, threadsClosure),
				    () => $"{nameof(mineAddress)}: {mineAddress}, {nameof(numberOfThreads)}: {threadsClosure}");
			    if (miner.Canceled)
				    break;
			    numberOfThreads = miner.Threads;// In case number of threads was canceled

		    } while (true);
	    }

	    public void MineStop()
	    {
		    if (_currentMiner != null)
		    {
			    _currentMiner.Stop();
			    _currentMiner = null;
		    }
	    }

		public async Task MineGenesisAsync(int? numberOfThreads)
	    {
		    if (_chainData.GetLastBlock() == null)
		    {
			    var genesisMiner = MinerFactory.Create(Genesis.GetBlockData(Cryptography, DateTime.UtcNow.Ticks), _feedback);
			    await _feedback.Execute("MineGenesis",
				    () => MineAsync(genesisMiner, numberOfThreads),
				    () => $"{nameof(numberOfThreads)}: {numberOfThreads}");
		    }
	    }

		private async Task MineAsync(Miner miner, int? numberOfThreads)
	    {
		    miner.Start(numberOfThreads ?? Environment.ProcessorCount);
		    _feedback.MineNewBlock(miner.Difficulty, miner.TargetBits);
			_currentMiner = miner;
			await AddMinedBlockAsync(miner);
	    }

	    private async Task AddMinedBlockAsync(Miner miner)
	    {
		    await _feedback.Execute("AddMinedBlockAsync",
			    async () =>
			    {
				    var minedBlock = await miner.GetBlock();
				    if (minedBlock != null)
				    {
					    _feedback.NewBlockMined(minedBlock.Signed.Data.Index, DateTime.UtcNow.Ticks - minedBlock.Signed.Data.TimeStamp);
					    AddNewBlock(minedBlock);
					    _currentMiner = null;
				    } else
					    _feedback.MinedBlockCanceled();
			    });
	    }

	    private void AddNewBlock(BlockHashed newBlock)
	    {
		    _feedback.Execute("AddNewBlock",
			    () =>
			    {
				    _chainData.AddNewBlock(newBlock);
				},
			    () => $"{nameof(newBlock)}: {newBlock.SerializeToJson()}");
		}

		public void SendTransaction(TransactionSigned result)
		{
			if (_chainData.AddPendingTransaction(result))
				_network.BroadcastAsync(new [] { result });
		}

		public Task AcceptTransactionsAsync(TransactionsBundle transactions)
		{
			foreach (var transaction in transactions.Transactions)
				_chainData.AddPendingTransaction(transaction);
			return Task.CompletedTask;
		}

		public Task AcceptBlockAsync(BlockBundle block)
		{
			_chainData.AddNewBlock(block.Block);
			return Task.CompletedTask;
		}

		public Task<string[]> ConnectToPeerAsync(string url)
		{
			return _network.ConnectToPeerAsync(url);
		}
	}
}
