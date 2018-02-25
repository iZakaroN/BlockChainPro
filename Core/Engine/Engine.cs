using System;
using System.Collections.Generic;
using System.Linq;
using BlockChanPro.Core.Contracts;

namespace BlockChanPro.Core.Engine
{
	public class BlockHashedIdentity
	{
			
	}
	//TODO: Split on smaller pieces, like 
    public class Engine
    {
		private readonly Cryptography _cryptography = new Cryptography();
		private List<BlockHashed> Chain { get; }
		//TODO: Order transactions in transaction.Sender->transaction.Id
		private List<Transaction> PendingTransactions { get; }

		public Engine()
	    {
		    Chain = new List<BlockHashed>();
		    PendingTransactions = new List<Transaction>();
		}

	    public BlockHashed Genesis()
	    {
			//Just start from beginning 
		    Chain.Clear();
		    var genesisBlock = _cryptography.ProcessBlock(BlockData.Genesis, Address.God, TargetHashBits.Genesis);

			Chain.Add(genesisBlock);
		    return genesisBlock;
	    }

	    public bool SendTransaction(Transaction transaction)
	    {
		    //TODO: Order transactions in transaction.Sender->transaction.Id, and eliminate duplicated transactions
		    PendingTransactions.Add(transaction);
		    return true;
	    }

	    public BlockHashed Mine(Address mineAddress)
	    {
		    var currentBlocks = Chain.Count;
		    if (currentBlocks == 0)
			    return null;
		    var lastBlock = Chain[currentBlocks - 1];

			var mineReward = new[]
		    {
			    new Transaction(Address.God, new[]
			    {
				    new Recipient(mineAddress, CalulateBlockReward(lastBlock)),
			    }, 0)
		    };
		    var blockToProcess = new BlockData(
			    lastBlock.Signed.Data.Index + 1,
			    DateTime.UtcNow.Ticks,
			    "Some message",
			    mineReward.Union(PendingTransactions).ToArray(),
			    lastBlock.Hash);

		    var targetHashBits = 
			    lastBlock.Signed.TargetHashBits.Adjust(blockToProcess.TimeStamp - lastBlock.Signed.Data.TimeStamp, BlockData.BlockTime);

		    var result = _cryptography.ProcessBlock(blockToProcess, mineAddress, targetHashBits);
			Chain.Add(result);
            PendingTransactions.Clear();
            return result;

	    }

	    private long CalulateBlockReward(BlockHashed lastBlock)
	    {
		    int rewardReduction = lastBlock.Signed.Data.Index / Transaction.BlockCountRewardReduction;

			return Transaction.GenesisReward >> rewardReduction;
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
