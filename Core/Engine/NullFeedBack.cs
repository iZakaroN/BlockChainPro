using BlockChanPro.Model.Contracts;

namespace BlockChanPro.Core.Engine
{
	public class NullFeedBack : IFeedback
	{
		public void MineNewBlock(long difficulty, HashBits targetBits)
		{
		}

		public void MiningStart(int threadsCount)
		{
		}

		public void NewBlockAccepted(int blockHeight, long blockTime, Hash blockHash)
		{
		}

		public void NewBlockMined(int blockHeight, long mineTime)
		{
		}

		public void NewBlockRejected(int blockHeight, long blockTime, Hash blockHash, string message)
		{
		}

		public void NewTransaction(TransactionSigned transaction)
		{
		}

		public void NewPeer(string peerUrl)
		{
		}

		public void SyncChainStart()
		{
		}

		public void SyncChainFinished()
		{
		}

		public void SyncChainAlreadyInSync()
		{
		}

		public void SyncChainProcessing(int syncStartBlockIndex, int latestBlockIndex, int peerCount)
		{
		}

		public void SyncChainRetrieveBlocks(int syncBlockIndex, int syncBlockPageSize, string hostAbsoluteUri)
		{
		}

		public void SyncChainPendingBlocks(int startIndex, int resultLength)
		{
		}

		public void SyncChainInvalidBlocks(int startIndex, int syncBlockPageSize)
		{
		}

		public void SyncChainProcessPendingBlocks(int startIndex, int syncBlockPageSize)
		{
		}

		public void MiningHashProgress(ulong hashesCalculated)
		{
		}

		public void MineCanceled()
		{
		}

		public void Start(string operation, string message)
		{
		}

		public void Stop(string operation, string message)
		{
		}

		public void Error(string operation, string message)
		{
		}
	}
}