using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BlockChanPro.Core.Engine.Data;
using BlockChanPro.Model.Contracts;
using Web.Shared;

namespace BlockChanPro.Core.Engine.Network
{
	public class P2PNetwork : IP2PNetwork
	{
		private readonly IFeedback _feedback;
		private readonly string _peerUrl;
		private readonly INetworkClientFactory _networkClientFactory;
		private readonly ConcurrentDictionary<string, PeerConnection> _peers = new ConcurrentDictionary<string, PeerConnection>();

		public P2PNetwork(IFeedback feedback, string peerUrl)
		{
			_feedback = feedback;
			_peerUrl = peerUrl;
			_networkClientFactory = new NetworkClientFactory();
		}

		public async Task<string[]> ConnectPeerAsync(string webAddress)
		{
			//Remove client from peers in case it was reconnecting
			var result = _peers.Keys.
				Where(p => string.Compare(p, webAddress, StringComparison.InvariantCultureIgnoreCase) != 0);
			await RegisterPeerAsync(webAddress);//Just connect the peer to your network
			await ConnectToPeerAsync(webAddress);//Or also discover new peers from him

			return result.ToArray();
		}

		public async Task<int> ConnectToPeerAsync(string webAddress)
		{
			var connection = await RegisterPeerAsync(webAddress);
			if (connection != null)
			{
				var peerConnections = await connection.Client.ConnectAsync(_peerUrl);
				foreach (var peerUrl in peerConnections)
				{
					try
					{
						//Prevent looping to self
						if (string.Compare(peerUrl, _peerUrl, StringComparison.InvariantCultureIgnoreCase) != 0)
							await ConnectToPeerAsync(peerUrl);
					}
					catch (Exception) {/*ignore*/}
				}
			}

			return _peers.Count;
		}

		private async Task<PeerConnection> RegisterPeerAsync(string url)
		{
			var client = _networkClientFactory.Create(url);
			await client.CheckAccessAsync();
			var address = client.Host.AbsoluteUri;
			var peerConnection = new PeerConnection(client);
			if (_peers.TryAdd(address, peerConnection))
			{
				_feedback.NewPeer(address);
				peerConnection.Accessible = true;
				return peerConnection;
			}

			return null;
		}

		public Task<string[]> GetConnectionsAsync()
		{
			return Task.FromResult(_peers.Keys.ToArray());
		}

		public Task BroadcastAsync(TransactionSigned[] transactions)
		{
			foreach (var peer in _peers.Values)
			{
				BroadcastToPeer(transactions, peer);
			}
			return Task.CompletedTask;
		}

		private void BroadcastToPeer(TransactionSigned[] transactions, PeerConnection peer)
		{
			var queuedTask = peer.Client.BroadcastAsync(
				new TransactionsBundle
				{
					Sender = _peerUrl,
					Transactions = transactions
				});
			FireBroadCast(peer, queuedTask);
		}

		public Task BroadcastAsync(BlockHashed block)
		{
			foreach (var peer in _peers.Values)
			{
				BroadcastToPeer(block, peer);
			}

			return Task.CompletedTask;
		}

		/*private class PeerBlockIdentity
		{
			public BlockIdentity Block { get; }
			public PeerConnection Peer { get; }

			public PeerBlockIdentity(BlockIdentity block, PeerConnection peer)
			{
				Block = block;
				Peer = peer;
			}
		}*/

		private class BlockPeers
		{
			public BlockIdentity Block { get; }
			public IEnumerable<PeerConnection> Peers { get; }

			public BlockPeers(BlockIdentity block, IEnumerable<PeerConnection> peers)
			{
				Block = block;
				Peers = peers;
			}
		}

		private async Task<SortedList<int, BlockPeers>> GetPeerLatestBlocks()
		{
			var result = new SortedList<int, BlockPeers>();
			foreach (var peer in _peers.Values)
			{
				try
				{
					var peerLatestBlockIdentity = await peer.Client.GetBlockIdentitiesAsync();
					if (peerLatestBlockIdentity.Length > 1)
					{
						await PeerConnectionError(peer, new Exception("Bad response: invalid number of blocks"));
					}
					else if (peerLatestBlockIdentity.Length > 0)
					{
						if (result.TryGetValue(peerLatestBlockIdentity[0].Height, out var exisitingPeers))
						{
							//Check if peer have same valid hash
							if (peerLatestBlockIdentity[0].Hash == exisitingPeers.Block.Hash)
								result[peerLatestBlockIdentity[0].Height] = 
									new BlockPeers(peerLatestBlockIdentity[0], exisitingPeers.Peers.Append(peer));
						} else //Add new height peer
							result.Add(
								peerLatestBlockIdentity[0].Height,
								new BlockPeers(peerLatestBlockIdentity[0],new [] { peer }));
					}
				}
				catch (Exception e)
				{
					await PeerConnectionError(peer, e);
				}
			}

			return result;
		}

		private async Task<BlockPeers> GetLatestBlockPeers()
		{
			var validPeers = new List<BlockPeers>();
			var peerLatestBlocks = await GetPeerLatestBlocks();
			var validateOrder = peerLatestBlocks.Select(l => l.Value).Reverse().ToList();
			while (validateOrder.Count > 0)
			{
				var current = validateOrder[0];
				validateOrder.RemoveAt(0);//For readability otherwise first peer have to be skipped

				//Retrieve other nodes last blocks to validate them 
				var peer = current.Peers.First();
				var currentHashes = validateOrder.Count == 0 ?
					new BlockIdentity[0] :
					await peer.Client.GetBlockIdentitiesAsync(validateOrder.Select(l => l.Block.Height).ToArray());
				//Remove peer if do not response do not contain proper number of blocks
				if (currentHashes.Length != validateOrder.Count)
				{
					await PeerConnectionError(peer, new Exception("Bad response: invalid number of blocks"));
				}
				else
				{
					validPeers.Add(current);
					var validatedPeers = new List<BlockPeers>();
					//Validate that peers with shorter chains has valid blocks according largest chain peer
					for (int i = 0; i < currentHashes.Length; i++)
						if (currentHashes[i].Hash == validateOrder[i].Block.Hash)
							validatedPeers.Add(validateOrder[i]);
					validateOrder = validatedPeers;
				}
			}
			if (validPeers.Count == 0)
				throw new BlockchainException("Cannot find pears with valid block chain");
			return new BlockPeers(
				validPeers[0].Block, 
				validPeers.Aggregate(
					(IEnumerable < PeerConnection >)new PeerConnection[] {},  
					(s,v) => s.Union(v.Peers)).ToArray());
		}

		private const int SyncBlockPageSizeMax = 3;

		public async Task BlockchainSync(IChainData chainData)
		{
			_feedback.SyncChainStart();
			var latestBlockPeers = await GetLatestBlockPeers();
			var networkLatestBlockIdentity = latestBlockPeers.Block;
			var peers = latestBlockPeers.Peers.ToArray();
			var localLastBlockIdentity = chainData.GetLastBlock().Identity();
			var localLastBlockIdentityHeight = localLastBlockIdentity?.Height ?? -1;
			//Check if network has blocks to sync
			if (localLastBlockIdentityHeight < networkLatestBlockIdentity.Height)
			{
				//Calculate optimal block to retrieve
				var blocksToSync = networkLatestBlockIdentity.Height - localLastBlockIdentityHeight;
				var syncBlockPageSize = Math.Min(SyncBlockPageSizeMax, blocksToSync / peers.Length + 1);
				var syncStartBlockIndex = localLastBlockIdentityHeight + 1;

				_feedback.SyncChainProcessing(syncStartBlockIndex, networkLatestBlockIdentity.Height, peers.Length);
				var chainSync = new ChainSync(syncStartBlockIndex, syncBlockPageSize, peers, chainData, _feedback);
				chainSync.Sync();

				_feedback.SyncChainFinished();
			}
			else
				_feedback.SyncChainAlreadyInSync();
		}

		//Fire and forget
		private void BroadcastToPeer(BlockHashed block, PeerConnection peer)
		{
			Debug.WriteLine($"Enqueue block {block.Signed.Data.Index}");
			var queuedTask = peer.Client.BroadcastAsync(new BlockBundle(block, _peerUrl));
			Debug.WriteLine($"Block {block.Signed.Data.Index} enqueued");
			FireBroadCast(peer, queuedTask);
		}

		private async void FireBroadCast(PeerConnection peer, Task queuedTask)
		{
			try
			{
				//TODO: Rebroadcast when implement limited connected peers
				//Send transaction only to other peers
				//if (string.Compare(peer.Key, _peerUrl, StringComparison.InvariantCultureIgnoreCase) != 0)
				{
					await queuedTask;
				}
			}
			catch (Exception e)
			{
				await PeerConnectionError(peer, e);
			}
		}

		private async Task PeerConnectionError(PeerConnection peer, Exception e)
		{
			_feedback.Error(nameof(BroadcastAsync), $"peer: {peer.Client.Host.AbsoluteUri}, message: {e.Message}");

			//TODO: Improve temporary errors by introducing some statistics
			await peer.DisconnectAsync();
			_peers.TryRemove(peer.Client.Host.AbsoluteUri, out var _); //remove dead client
		}
	}
}
