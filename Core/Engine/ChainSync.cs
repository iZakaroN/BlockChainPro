using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockChanPro.Core.Engine.Data;
using BlockChanPro.Core.Engine.Network;
using BlockChanPro.Model.Contracts;

namespace BlockChanPro.Core.Engine
{
	public class ChainSync
	{
		private int _syncBlockIndexCap;
		private int _syncBlockIndex;
		private readonly int _syncBlockPageSize;
		private readonly PeerConnection[] _peers;
		private readonly IChainData _chainData;
		private readonly IFeedback _feedback;
		private readonly SortedList<int, SyncBlocksTask> _pendingBlocks = new SortedList<int, SyncBlocksTask>();
		private readonly SortedList<int, SyncBlocksTask> _missingBlocks = new SortedList<int, SyncBlocksTask>();

		public ChainSync(
			int syncBlockIndex,
			int syncBlockPageSize,
			PeerConnection[] peers,
			IChainData chainData,
			IFeedback feedback)
		{
			_syncBlockIndexCap = _syncBlockIndex = syncBlockIndex;
			_syncBlockPageSize = syncBlockPageSize;
			_peers = peers;
			_chainData = chainData;
			_feedback = feedback;
		}

		public void Sync()
		{
			var retrieveBlocksTasks = new SyncBlocksTask[_peers.Length];
			for (var peerIndex = 0; peerIndex < _peers.Length; peerIndex++)
			{
				retrieveBlocksTasks[peerIndex] = StartPeerSync(peerIndex);
			}

			do
			{
				var tasks = retrieveBlocksTasks.Select(rbt => rbt.Task).ToArray();
				// ReSharper disable once CoVariantArrayConversion
				Task.WaitAny(tasks);
				foreach (var peerTask in retrieveBlocksTasks)
					ProcessPeerBlocks(peerTask);

				ProcessPendingBlocks();
			} while (SyncBlocks(retrieveBlocksTasks));
			//Continue only if block is full, otherwise peer do not have more blocks
			/*if (retrieveBlocksTask.Task.Result.Length >= _syncBlockPageSize)
			{
				retrieveBlocksTasks[completeTaskIndex] = StartPeerSync(completeTaskIndex);
				return true;
			}*/
		}

		private void ProcessPendingBlocks()
		{
			while (_pendingBlocks.Count > 0)
			{
				var peerBlocksTask = _pendingBlocks.First().Value;
				var peerBlockNextIndex = CalculatePeerBlockNextIndex(peerBlocksTask.StartIndex);
				if (peerBlockNextIndex >= 0)
				{
					_pendingBlocks.RemoveAt(0);
					_feedback.SyncChainProcessPendingBlocks(peerBlocksTask.StartIndex, _syncBlockPageSize);
					if (AddNewChainBlocks(peerBlockNextIndex, peerBlocksTask.Task.Result))
						return;

					FailPeerBlocks(peerBlocksTask);
				}
				else
					break;
			}
		}

		private bool SyncBlocks(SyncBlocksTask[] peerTasks)
		{
			bool activeTasks = false;
			for (var peerTaskIndex = 0; peerTaskIndex < peerTasks.Length; peerTaskIndex++)
			{
				if (peerTasks[peerTaskIndex].Ready && peerTasks[peerTaskIndex].Processed)
				{
					if (_missingBlocks.Count > 0)
					{
						var missingBlocksTask = _missingBlocks[0];
						_missingBlocks.RemoveAt(0);
						peerTasks[peerTaskIndex] = StartPeerSync(peerTaskIndex, missingBlocksTask.StartIndex, missingBlocksTask.PageSize);
						//activeTasks = true;
					}
					else if (!peerTasks[peerTaskIndex].Completed)
					{
						peerTasks[peerTaskIndex] = StartPeerSync(peerTaskIndex);
						//activeTasks = true;
					}

				}
				activeTasks = activeTasks || !peerTasks[peerTaskIndex].Processed;
			}

			return activeTasks || _pendingBlocks.Count > 0;
		}

		/// <summary>
		/// Try add retrieved peer blocks to block chain or add them as failed to be retried from another peer
		/// </summary>
		/// <param name="peerBlocksTask"></param>
		/// <returns>True if peer are processed successfully</returns>
		private void ProcessPeerBlocks(SyncBlocksTask peerBlocksTask)
		{
			if (peerBlocksTask.Ready)
			{
				if (peerBlocksTask.AwaitProcessing)
				{
					if (!TryAddBlocks(peerBlocksTask))
						FailPeerBlocks(peerBlocksTask);
				}

				peerBlocksTask.Processed = true;
			}
		}

		private void FailPeerBlocks(SyncBlocksTask peerBlocksTask)
		{
			_feedback.SyncChainInvalidBlocks(peerBlocksTask.StartIndex, _syncBlockPageSize);
			peerBlocksTask.Failed = true;
			_missingBlocks.Add(peerBlocksTask.StartIndex, peerBlocksTask);
		}

		private bool TryAddBlocks(SyncBlocksTask peerBlocksTask)
		{
			var peerResult = peerBlocksTask.Task.Result;
			if (peerBlocksTask.StartIndex != peerResult[0].Signed.Data.Index)
				return false;

			var peerBlockNextIndex = CalculatePeerBlockNextIndex(peerBlocksTask.StartIndex);
			if (peerBlockNextIndex >= 0)
				return AddNewChainBlocks(peerBlockNextIndex, peerResult);
			//Add as pending blocks, as previous blocks are still not synced
			_feedback.SyncChainPendingBlocks(peerBlocksTask.StartIndex, peerBlocksTask.Task.Result.Length);
			_pendingBlocks.Add(peerBlocksTask.StartIndex, peerBlocksTask);
			return true;
		}

		private bool AddNewChainBlocks(int peerBlockNextIndex, BlockHashed[] peerResult)
		{
			//Only add blocks that are not synced yet. They can be partially synced from previous host
			var peerIndexCap = 0;
			for (; peerBlockNextIndex < peerResult.Length; peerBlockNextIndex++)
			{
				var peerResultBlock = peerResult[peerBlockNextIndex];
				if (_chainData.AddNewBlock(peerResultBlock) != BlockchainState.Healty)
					return false;
				peerIndexCap = peerResultBlock.Signed.Data.Index + 1;
			}

			//Find larger index to be current sync cap
			if (_syncBlockIndexCap < peerIndexCap)
				_syncBlockIndexCap = peerIndexCap;
			else //Find a hole in block chunks as there is already larger block height retrieved
			{
				var missingChunkBlocks = _syncBlockPageSize - peerResult.Length;
				if (missingChunkBlocks > 0)
				{
					_missingBlocks.Add(
						peerIndexCap,
						new SyncBlocksTask(
							peerIndexCap,
							missingChunkBlocks,
							null));
				}
			}
			return true;
		}

		private int CalculatePeerBlockNextIndex(int peerBlockStartIndex)
		{
			var nextBlockIndex = (_chainData.GetLastBlock()?.Signed.Data.Index ?? -1) + 1;
			var nextPeerBlockIndex = nextBlockIndex - peerBlockStartIndex;
			return nextPeerBlockIndex;
		}

		private SyncBlocksTask StartPeerSync(int peerIndex)
		{
			var result = StartPeerSync(peerIndex, _syncBlockIndex, _syncBlockPageSize);
			_syncBlockIndex += _syncBlockPageSize;
			return result;
		}

		private SyncBlocksTask StartPeerSync(int peerIndex, int syncBlockIndex, int syncBlockPageSize)
		{
			_feedback.SyncChainRetrieveBlocks(syncBlockIndex, syncBlockPageSize, _peers[peerIndex].Client.Host.AbsoluteUri);
			var result = new SyncBlocksTask(
				syncBlockIndex,
				syncBlockPageSize,
				_peers[peerIndex].Client.GetBlocksAsync(syncBlockIndex, syncBlockPageSize));
			return result;
		}
	}
}
