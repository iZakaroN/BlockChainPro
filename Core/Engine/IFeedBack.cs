using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BlockChanPro.Model.Contracts;

namespace BlockChanPro.Core.Engine
{
	public interface IFeedback
	{
		void Start(string operation, string message);
		void Stop(string operation, string message);
		void Error(string operation, string message);

		void MiningStart(int threadsCount);
		void MiningHashProgress(ulong hashesCalculated);
		void MineNewBlock(long difficulty, HashBits targetBits);
		void MineCanceled();

		void NewBlockAccepted(int blockHeight, long blockTime, Hash blockHash);
		void NewBlockMined(int blockHeight, long mineTime);
		void NewBlockRejected(int blockHeight, long blockTime, Hash blockHash, string message);
		void NewTransaction(TransactionSigned transaction);
		void NewPeer(string peerUrl);

		void SyncChainStart();
		void SyncChainFinished();
		void SyncChainAlreadyInSync();
		void SyncChainProcessing(int syncStartBlockIndex, int latestBlockIndex, int peerCount);
		void SyncChainRetrieveBlocks(int syncBlockIndex, int syncBlockPageSize, string hostAbsoluteUri);
		void SyncChainPendingBlocks(int startIndex, int resultLength);
		void SyncChainInvalidBlocks(int startIndex, int syncBlockPageSize);
		void SyncChainProcessPendingBlocks(int startIndex, int syncBlockPageSize);
	}

	public static class FeedbackExtensions
	{
		public static async Task Execute(this IFeedback feedback, string operationName, Func<Task> operation, Func<string> logParameters = null)
		{
			try
			{
				feedback.Start(operationName, logParameters?.Invoke());
				var sw = Stopwatch.StartNew();
				await operation();
				feedback.Stop(operationName, $" /{sw.ElapsedMilliseconds}ms");
			}
			catch (Exception e)
			{
				feedback.Error(operationName, e.Message);
			}
		}

		public static void Execute(this IFeedback feedback, string operationName, Action operation, Func<string> logParameters = null)
		{
			try
			{
				feedback.Start(operationName, logParameters?.Invoke());
				var sw = Stopwatch.StartNew();
				operation();
				feedback.Stop(operationName, $" /{sw.ElapsedMilliseconds}ms");
			}
			catch (Exception e)
			{
				feedback.Error(operationName, e.Message);
			}
		}

		public static async Task<T> Execute<T>(this IFeedback feedback, string operationName, Func<Task<T>> operation, Func<string> logParameters = null)
		{
			try
			{
				feedback.Start(operationName, logParameters?.Invoke());
				var sw = Stopwatch.StartNew();
				var result = await operation();
				feedback.Stop(operationName, $"=> {result} /{sw.ElapsedMilliseconds}ms");
				return result;
			}
			catch (Exception e)
			{
				feedback.Error(operationName, e.Message);
				throw;
			}
		}

		public static T Execute<T>(this IFeedback feedback, string operationName, Func<T> operation, Func<string> logParameters = null)
		{
			try
			{
				feedback.Start(operationName, logParameters?.Invoke());
				var sw = Stopwatch.StartNew();
				var result =  operation();
				feedback.Stop(operationName, $"=> {result} /{sw.ElapsedMilliseconds}ms");
				return result;
			}
			catch (Exception e)
			{
				feedback.Error(operationName, e.Message);
				throw;
			}
		}

	}
}