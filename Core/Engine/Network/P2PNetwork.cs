using System;
using System.Collections.Concurrent;
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
					await peer.Value.Client.BroadcastAsync(
						new TransactionsBundle
						{
							Sender = _peerUrl,
							Transactions = transactions
						});
				}
			}
		}

		public async Task BroadcastAsync(BlockHashed block)
		{
			foreach (var peer in _peers)
			{
				//TODO: Rebroadcast when implement limited connected peers
				//Send transaction only to other peers
				//if (string.Compare(peer.Key, _peerUrl, StringComparison.InvariantCultureIgnoreCase) != 0)
				{
					await peer.Value.Client.BroadcastAsync(new BlockBundle(block, _peerUrl));
				}
			}
		}
	}
}
