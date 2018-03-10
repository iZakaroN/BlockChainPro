using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BlockChanPro.Model.Contracts;
using BlockChanPro.Model.Interfaces;

namespace Web.Shared
{
	public class NetworkClient : INetworkClient
	{
		private readonly HttpClient _httpClient;
		private readonly SemaphoreSlim _broadcastSlots = new SemaphoreSlim(0);
		private readonly ConcurrentQueue<Task> _broadcastQueue = new ConcurrentQueue<Task>();
		private readonly Task _broadcastTask;
		private readonly CancellationTokenSource _broadcaseCancelation = new CancellationTokenSource();
		public NetworkClient(HttpClient httpClient)
		{
			_httpClient = httpClient;
			_broadcastTask = WaitForBroadcast();
		}

		private int _tasksExecuted = 0;
		public async Task WaitForBroadcast()
		{
			while (!_broadcaseCancelation.Token.IsCancellationRequested)
			{
				await _broadcastSlots.WaitAsync();
				if (_broadcastQueue.TryDequeue(out var broadcastTask))
				{
					var tasksExecuted = Interlocked.Increment(ref _tasksExecuted);
					Debug.WriteLine($"Executing queue number {tasksExecuted}");
					broadcastTask.Start();
					try
					{
						await broadcastTask;
					}
					catch (Exception) { /* Just ignore and continue with the next queued task. QueueBroadcast will re-translate it*/}

					Debug.WriteLine($"Queue task number {tasksExecuted} executed");
				}
				else
					Debug.WriteLine("There was a broadcast slot but not a broadcast task!");
			}

		}

		private int _tasksEnqueued = 0;
		private Task QueueBroadcast(Func<Task> broadcastTaskFactory)
		{
			if (!_broadcaseCancelation.IsCancellationRequested)
			{
				var broadcastTaskSlot = new SemaphoreSlim(0);
				var broadcastTaskSync = SyncTaskExecute(broadcastTaskSlot, broadcastTaskFactory);
				var enqueueNumber = Interlocked.Increment(ref _tasksEnqueued);
				Debug.WriteLine($"{enqueueNumber}> Enqueue number {_broadcastQueue.Count}");
				_broadcastQueue.Enqueue(broadcastTaskSync);
				Debug.WriteLine($"{enqueueNumber}> {_broadcastQueue.Count} enqueued");
				_broadcastSlots.Release();
				return SyncTaskResult(broadcastTaskSlot, broadcastTaskSync);
			}

			return Task.CompletedTask;
		}

		private Task SyncTaskExecute(SemaphoreSlim taskSync, Func<Task> taskFactory)
		{
			//Use new task so task will start from the queue
			//TODO: Any way to use await instead of .Wait()
			return new Task(() =>
			{
				taskSync.Release();

				taskFactory().Wait();
			});
		}

		private async Task SyncTaskResult(SemaphoreSlim taskSync, Task task)
		{
			using (taskSync)
			{
				await taskSync.WaitAsync();
				await task;
			}
		}


		public async Task<string> GetVersionAsync()
		{
			var response = await _httpClient.GetAsync(ApiConstants.Root);
			response.EnsureSuccessStatusCode();
			return await response.Content.ReadAsJsonAsync<string>();
		}

		public async Task<string[]> GetConnectionsAsync()
		{
			var response = await _httpClient.GetAsync(ApiConstants.Connections);
			response.EnsureSuccessStatusCode();
			return await response.Content.ReadAsJsonAsync<string[]>();
		}

		public async Task<string[]> ConnectAsync(string senderUri)
		{
			var response = await _httpClient.PostAsJsonAsync(ApiConstants.Connections, senderUri);
			response.EnsureSuccessStatusCode();
			return await response.Content.ReadAsJsonAsync<string[]>();
		}

		public Task BroadcastAsync(TransactionsBundle transactions) =>
			QueueBroadcast(() => BroadCastExecuteAsync(transactions));

		private async Task BroadCastExecuteAsync(TransactionsBundle transactions)
		{
			var response = await _httpClient.PostAsJsonAsync(ApiConstants.Transactions, transactions);
			DebugResponse(response);
		}

		public Task BroadcastAsync(BlockBundle block) =>
			QueueBroadcast(() => BroadCastExecuteAsync(block));

		private async Task BroadCastExecuteAsync(BlockBundle block)
		{
			Debug.WriteLine($"Sending block {block.Block.Signed.Data.Index}");
			var response = await _httpClient.PostAsJsonAsync(ApiConstants.Blocks, block);
			Debug.WriteLine($"Block {block.Block.Signed.Data.Index} sent");
			DebugResponse(response);
		}

		public Uri Host => _httpClient.BaseAddress;

		public async Task CheckAccessAsync()
		{
			var version = await GetVersionAsync();
			if (version != ApiConstants.Version)
				throw new ApiException($"Peer has invalid api version 'v{version}'. Expected '{ApiConstants.Version}'");
		}

		public async Task DisposeAsync()
		{
			_broadcaseCancelation.Cancel();
			await _broadcastTask;
			//return _broadcastTask; //return broadcast task in case you want to await it in caller

		}

		private void DebugResponse(HttpResponseMessage response, [CallerMemberName]string caller = null)
		{
			Debug.WriteLine($"{_httpClient.BaseAddress}/{caller} => {response.StatusCode}");
		}
	}
}
