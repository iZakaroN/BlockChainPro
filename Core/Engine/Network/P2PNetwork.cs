using System;
using System.Collections.Concurrent;
using System.Linq;
using BlockChanPro.Model.Contracts;
using Web.Shared;

namespace BlockChanPro.Core.Engine.Network
{
    public class P2PNetwork : IP2PNetwork
    {
	    private readonly INetworkClientFactory _networkClientFactory;
	    private ConcurrentDictionary<string, PeerConnection> _peers = new ConcurrentDictionary<string, PeerConnection>();

	    public P2PNetwork()
	    {
		    _networkClientFactory = new NetworkClientFactory();
	    }

		public string[] Connect(string webAddress)
	    {
		    var result = GetConnections();
		    RegisterConnection(webAddress);
		    return result;
	    }

	    private void RegisterConnection(string url)
	    {
		    var client = _networkClientFactory.Create(url);
		    var address = client.Host.AbsoluteUri;
		    var peerConnection = new PeerConnection(client);
			if (_peers.TryAdd(address, peerConnection))
		    {
			    CheckAccess(address);
			    peerConnection.Accessible = true;


		    }
	    }

	    private void CheckAccess(string url)
	    {
		    throw new NotImplementedException();
	    }

		public string[] GetConnections()
	    {
		    return _peers.Keys.ToArray();
	    }

	    public void Broadcast(BlockHashed block, string peerUri = null)
	    {
		    throw new NotImplementedException();
	    }

	    public void Broadcast(TransactionSigned[] transactions, string sender = null)
	    {
		    foreach (var peer in _peers)
		    {
				//Send transaction only to other peers
			    if (string.Compare(peer.Key, sender, StringComparison.InvariantCultureIgnoreCase) != 0)
			    {
				    peer.Value.Client.BroadcastAsync(transactions);
			    }

		    }

		}
    }
}
