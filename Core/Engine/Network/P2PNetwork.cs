using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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

		public async Task BroadcastAsync(TransactionSigned[] transactions)
		{
			foreach (var peer in _peers)
			{
				//TODO: Rebroadcast when implement limited connected peers
				//Send transaction only to other peers
				//if (string.Compare(peer.Key, _peerUrl, StringComparison.InvariantCultureIgnoreCase) != 0)
				{
					try
					{
						await peer.Value.Client.BroadcastAsync(
							new TransactionsBundle
							{
								Sender = _peerUrl,
								Transactions = transactions
							});
					}
					catch (Exception e)
					{
						_feedback.Error(nameof(BroadcastAsync), $"peer: {peer.Value.Client.Host.AbsoluteUri}, message: {e.Message}");

					}
				}
			}
		}

		public Task BroadcastAsync(BlockHashed block)
		{
			foreach (var peer in _peers.Values)
			{
				BroadcastToPeer(block, peer);
			}

			return Task.CompletedTask;
		}

		//Fire and forget
		//TODO: Queue client operations as currently rapid requests do not arrive in the order they have been send because of fire and forget
		private void BroadcastToPeer(BlockHashed block, PeerConnection peer)
		{
			Debug.WriteLine($"Enqueue block {block.Signed.Data.Index}");
			var queuedTask = peer.Client.BroadcastAsync(new BlockBundle(block, _peerUrl));
			Debug.WriteLine($"Block {block.Signed.Data.Index} enqueued");
			Fire(peer, queuedTask);
		}

		private async void Fire(PeerConnection peer, Task queuedTask)
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
				_feedback.Error(nameof(BroadcastAsync), $"peer: {peer.Client.Host.AbsoluteUri}, message: {e.Message}");
				//TODO: As client can be not accessible only temporary improve dead clients handling by access time stamp and clean up thread. 
				//peer.Accessible = false;
				await peer.DisconnectAsync();
				_peers.TryRemove(peer.Client.Host.AbsoluteUri, out var _); //remove dead client
			}
		}
	}
}
