using BlockChanPro.Model.Contracts;

namespace BlockChanPro.Core.Engine
{
	public class NullFeedBack : IFeedback
	{
		public void MineNewBlock(long difficulty, HashBits targetBits)
		{
		}

		public void StartProcess(int threadsCount)
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

		public void StartBlockchainSync()
		{
		}

		public void HashProgress(ulong hashesCalculated)
		{
		}

		public void MinedBlockCanceled()
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