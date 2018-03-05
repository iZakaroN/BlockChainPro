using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockChanPro.Core.Contracts;
using BlockChanPro.Core.Serialization;

namespace BlockChanPro.Core.Engine
{
	//TODO: Split on smaller pieces
	public class Engine
    {
	    private readonly IFeedBack _feedback;

	    //TODO: inject from factory
		private static readonly Cryptography Cryptography = new Cryptography();
		private static readonly MinerFactory MinerFactory = new MinerFactory(Cryptography);

		private List<BlockHashed> Chain { get; }
		//TODO: Order transactions in transaction.Sender->transaction.Id
		private ConcurrentDictionary<Hash, TransactionSigned> PendingTransactions { get; }
	    private Miner _currentMiner;
	    // ReSharper disable once NotAccessedField.Local //Just in case
	    private Task _minerTask;

		public Engine(IFeedBack feedback)
	    {
		    _feedback = feedback;
		    Chain = new List<BlockHashed>();
			PendingTransactions = new ConcurrentDictionary<Hash, TransactionSigned>();
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
			    var currentBlocks = Chain.Count;
			    var lastBlock = Chain[currentBlocks - 1];
			    var miner = MinerFactory.Create(mineAddress, lastBlock, SelectTransactionsToMine(), _feedback);
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
		    if (Chain.Count == 0)
		    {
			    var genesisMiner = MinerFactory.Create(Address.God, BlockData.Genesis, HashBits.GenesisTarget, _feedback);
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
					long blockTime = 0;
				    if (Chain.Count > 0)
				    {
					    var lastBlock = Chain[Chain.Count - 1];

						if (newBlock.Signed.Data.PreviousHash != lastBlock.HashTarget.Hash)
						    throw new Exception("New block is not valid for current chain state");
					    blockTime = newBlock.Signed.Data.TimeStamp - lastBlock.Signed.Data.TimeStamp;

				    }

				    foreach (var blockTransactions in newBlock.Signed.Data.Transactions)
					    PendingTransactions.TryRemove(blockTransactions.Sign, out var _);
				    _feedback.NewBlockFound(newBlock.Signed.Data.Index, blockTime, newBlock.HashTarget.Hash);

					Chain.Add(newBlock);
			    },
			    () => $"{nameof(newBlock)}: {newBlock.SerializeToJson()}");
		}

	    private IEnumerable<TransactionSigned> SelectTransactionsToMine()
	    {
		    //Just choose add all transactions
		    return PendingTransactions.Values;
	    }

	    public bool SendTransaction(TransactionSigned transaction)
	    {
			return PendingTransactions.TryAdd(transaction.Sign, transaction);
	    }

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

		public TransactionsInfo CalulateTransactionsInfo()
		{
			return new TransactionsInfo(
				Chain.Aggregate(0, (seed, block) =>  seed + block.Signed.Data.Transactions.Length),
				PendingTransactions.Count
			);
	    }
	}
}
