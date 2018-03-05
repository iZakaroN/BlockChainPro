using System;
using System.Threading;
using System.Threading.Tasks;
using BlockChanPro.Core.Contracts;

namespace BlockChanPro.Core.Engine
{
	public class MinerTask : IDisposable
	{
		public MinerTask(CancellationTokenSource cancellationToken)
		{
			Cancellation = cancellationToken;
		}

		public Task<HashTarget> Task { get; set; }
		private CancellationTokenSource Cancellation { get; }

		public bool IsCompleted => Task.IsCompleted;
		public bool IsCanceled => Task.IsCanceled;
		public HashTarget Result => Task.IsCompletedSuccessfully ? Task.Result : null;

		public void Start(Func<CancellationToken, HashTarget> execute)
		{
			Task?.Dispose();
			var a = TaskScheduler.Default.MaximumConcurrencyLevel;
			Task = System.Threading.Tasks.Task.Factory.StartNew(() =>
				execute(Cancellation.Token),
				Cancellation.Token);
		}

		public void Stop()
		{
			Cancellation.Cancel();
			try
			{
				Task.Wait();
			} catch(Exception)
			{ /*just ignore OperationCanceledException.*/}

			Dispose();
		}


		public void Dispose()
		{
			Task?.Dispose();
			Cancellation?.Dispose();
		}
	}
}