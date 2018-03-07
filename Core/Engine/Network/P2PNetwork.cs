using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using BlockChanPro.Model.Contracts;
using Web.Shared;

namespace BlockChanPro.Core.Engine.Network
{
    public class P2PNetwork : IP2PNetwork
    {
	    private readonly string _peerUrl;
	    private readonly INetworkClientFactory _networkClientFactory;
	    private ConcurrentDictionary<string, PeerConnection> _peers = new ConcurrentDictionary<string, PeerConnection>();

	    public P2PNetwork(string peerUrl)
	    {
		    _peerUrl = peerUrl;
		    _networkClientFactory = new NetworkClientFactory();
	    }

		public async Task<string[]> ConnectAsync(string webAddress)
	    {
		    var result = await GetConnectionsAsync();
		    await RegisterConnectionAsync(webAddress);
		    return result;
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

	    public async Task Broadcast(BlockHashed block)
	    {
		    foreach (var peer in _peers)
		    {
			    //TODO: Rebroadcast when implement limited connected peers
			    //Send transaction only to other peers
				//if (string.Compare(peer.Key, _peerUrl, StringComparison.InvariantCultureIgnoreCase) != 0)
				{
					await peer.Value.Client.BroadcastAsync(
					    new BlockBundle
					    {
						    Sender = _peerUrl,
						    Block = block
					    });
			    }
		    }
	    }

	    private async Task RegisterConnectionAsync(string url)
	    {
		    var client = _networkClientFactory.Create(url);
		    await client.CheckAccessAsync();
		    var address = client.Host.AbsoluteUri;
		    var peerConnection = new PeerConnection(client);
		    if (_peers.TryAdd(address, peerConnection))
		    {
			    peerConnection.Accessible = true;


		    }
	    }


	}
}
