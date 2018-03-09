using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BlockChanPro.Model.Contracts;

namespace BlockChanPro.Core.Engine
{
	public interface IFeedBack
	{
		void MineNewBlock(long difficulty, HashBits targetBits);
		void StartProcess(int threadsCount);

		void NewBlockAccepted(int blockHeight, long blockTime, Hash blockHash);
		void NewBlockMined(int blockHeight, long mineTime);
		void NewBlockRejected(int blockHeight, long blockTime, Hash blockHash, string message);
		void NewTransaction(TransactionSigned transaction);

		void HashProgress(ulong hashesCalculated);
		void MinedBlockCanceled();
		void Start(string operation, string message);
		void Stop(string operation, string message);
		void Error(string operation, string message);
	}

	public static class FeedbackExtensions
	{
		public static async Task Execute(this IFeedBack feedback, string operationName, Func<Task> operation, Func<string> logParameters = null)
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

		public static void Execute(this IFeedBack feedback, string operationName, Action operation, Func<string> logParameters = null)
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

		public static async Task<T> Execute<T>(this IFeedBack feedback, string operationName, Func<Task<T>> operation, Func<string> logParameters = null)
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

		public static T Execute<T>(this IFeedBack feedback, string operationName, Func<T> operation, Func<string> logParameters = null)
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