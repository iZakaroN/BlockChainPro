using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BlockChanPro.Core.Contracts;

namespace BlockChanPro.Core.Engine
{
	//TODO: Split on smaller pieces
    public class Engine
    {
        //TODO: inject
		private static readonly Cryptography Cryptography = new Cryptography();
		private static readonly MinerFactory MinerFactory = new MinerFactory(Cryptography);

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
			var genesisMiner = MinerFactory.Create(Address.God, BlockData.Genesis, HashBits.GenesisTarget); 
		    var genesisBlock = genesisMiner.Start(new CancellationToken());

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

		    var miner = MinerFactory.Create(mineAddress, lastBlock, PendingTransactions);
		    var block = miner.Start(new CancellationToken());

			Chain.Add(block);
            PendingTransactions.Clear();
            return block;

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
