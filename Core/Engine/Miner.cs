using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlockChanPro.Model.Contracts;
using BlockChanPro.Model.Serialization;
using BlockChanPro.Core.Contracts;

namespace BlockChanPro.Core.Engine
{
	//TODO: Split into server/client for pool mining
	//TODO: Tests
	public class Miner
	{
		//Increasing the value will reduce nounce switch, but will increase holes in the nounce when threads fail, or swithced on/off
		private const ulong NounceStep = 100000;
		private ulong _nextNounce;
		private readonly Queue<MinerTask> _tasks = new Queue<MinerTask>();
		private readonly CancellationTokenSource _taskManagerWork;
		private CancellationTokenSource _taskManagerWait;
		private Task<HashTarget> _taskManager;

		private readonly Hash _hashTarget;
		//TODO: move to server miner
		private readonly BlockSigned _signedBlock;
		private readonly byte[] _signedBlockBytes;

		//TODO: Change with factory to separate crypto use with every miner thread
		// ReSharper disable once NotAccessedField.Local //See todo
		private readonly Cryptography _cryptography;
		private readonly IFeedBack _feedback;
		
		public Address Address => _signedBlock.Stamp;

		public Miner(
			HashBits hashTargetBits, 
			BlockSigned signedBlock, 
			Hash signedBlockHash, 
			Cryptography cryptography,
			IFeedBack feedback)
		{
			_hashTarget = hashTargetBits.ToHash();
			_signedBlock = signedBlock;
			_signedBlockBytes = signedBlockHash.ToBinary();
			_cryptography = cryptography;
			_feedback = feedback;

			_taskManagerWork = new CancellationTokenSource();
			InitializeTaskManagerWait();
			_taskManager = new Task<HashTarget>(ManageTasks, _taskManagerWork.Token);

		}

		/// <summary>
		/// Start specific number of miner threads
		/// Can be called multiple times to reduce or increase the number of threads
		/// </summary>
		/// <param name="taskCount">Number of parallel miners to start</param>
		/// <returns></returns>
		public void Start(int taskCount)
		{
			if (taskCount > _tasks.Count)
				AddTasks(taskCount - _tasks.Count);
			else if (taskCount < _tasks.Count)
				RemoveTasks(_tasks.Count - taskCount);
			Threads = taskCount;
			RefreshTaskManager();
		}

		public bool Canceled { get; private set; }
		public long Difficulty => _signedBlock.HashTargetBits.Difficulty(Genesis.Target);
		public HashBits TargetBits => _signedBlock.HashTargetBits;
		public int Threads { get; internal set; }

		public void Stop()
		{
			Canceled = true;
			CancelTaskManager();
		}

		private void RemoveTasks(int taskCount)
		{
			_feedback.Execute("RemoveTasks",
				() =>
				{
					var tasksToRemove = new List<MinerTask>();
					for (var i = 0; i < taskCount; i++)
					{
						tasksToRemove.Add(_tasks.Dequeue());
					}

					StopTasks(tasksToRemove);
				},
				() => $"{nameof(taskCount)}: {taskCount}");
		}

		private static void StopTasks(IEnumerable<MinerTask> tasksToRemove)
		{
			foreach (var minerTask in tasksToRemove)
				minerTask.Stop();
		}

		private void AddTasks(int taskCount)
		{
			_feedback.Execute("AddTasks",
				() =>
				{
					for (var i = 0; i < taskCount; i++)
					{
						var cancelationTokenSource = new CancellationTokenSource();
						var task = new MinerTask(cancelationTokenSource);
						var nounce = _nextNounce;
						task.Start(cancellationToken =>
							FindHashTarget(
								new HashBits(HashBits.OffsetMax, nounce),
								NounceStep,
								cancellationToken));
						_tasks.Enqueue(task);
						_nextNounce += NounceStep;
					}
				},
				() => $"{nameof(taskCount)}: {taskCount}");
		}

		private HashTarget FindHashTarget(HashBits initialNounce, ulong maxItterations, CancellationToken cancellationToken)
		{
			return _feedback.Execute("FindHashTarget",
				() =>
				{
					var cryptography = new Cryptography();
					ulong itterations = 0;
					try
					{
						var nounce = initialNounce.ToHash();
						do
						{
							var hash = cryptography.CalculateHash(_signedBlockBytes, nounce);
							if (hash.Compare(_hashTarget) < 0)
								return new HashTarget(nounce, hash);
							nounce.Increment(1);
						} while (++itterations < maxItterations && !cancellationToken.IsCancellationRequested);

						return null;
					}
					finally
					{
						_feedback.HashProgress(itterations);
					}
				},
				() => $"{nameof(initialNounce)}: {initialNounce}, {nameof(maxItterations)}: {maxItterations}");
		}

		public async Task<BlockHashed> GetBlock()
		{
			var result = await _taskManager;
			StopTasks(_tasks);
			//_signedBlock.Data.TimeStamp = DateTime.UtcNow.Ticks;
			return result != null ? new BlockHashed(_signedBlock, result) : null;
		}

		private void CancelTaskManager()
		{
			_taskManagerWork?.Cancel();
			_taskManager?.Wait();
			_taskManagerWork?.Dispose();
			_taskManagerWait?.Dispose();
			_taskManager = null;
		}

		private void RefreshTaskManager()
		{
			if (_taskManager.Status == TaskStatus.Created)
			{
				_taskManager.Start();
			}
			else
			{
				_taskManagerWait.Cancel();
			}

		}

		private void InitializeTaskManagerWait()
		{
			_taskManagerWait = CancellationTokenSource.CreateLinkedTokenSource(_taskManagerWork.Token);
		}

		private HashTarget ManageTasks()
		{
			while (!_taskManagerWork.IsCancellationRequested)
			{
				// ReSharper disable once CoVariantArrayConversion //Not modified
				Task[] tasks = _tasks.Select(mt => mt.Task).ToArray();
				_feedback.StartProcess(tasks.Length);
				try
				{
					Task.WaitAny(tasks, _taskManagerWait.Token);
					var tasksRefreshed = 0;
					while (!_taskManagerWork.IsCancellationRequested && tasksRefreshed++ < tasks.Length)
					{
						if (_tasks.TryDequeue(out var minerTask))
						{
							if (minerTask.IsCompleted)
							{
								if (!minerTask.IsCanceled)
								{
									AddTasks(1);
									if (minerTask.Result != null)
										return minerTask.Result;

								}
							}
							else
								_tasks.Enqueue(minerTask);
						}
						else
							break;
					}
				}
				catch (Exception e)
				{
					_feedback.Error("ManageTasks", e.Message);
					if (_taskManagerWait.IsCancellationRequested)
					{
						if (!_taskManagerWork.IsCancellationRequested)
							InitializeTaskManagerWait();
					}
				}
			}

			return null;
		}

	}
}
