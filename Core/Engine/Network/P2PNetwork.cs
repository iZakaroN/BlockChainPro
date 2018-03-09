﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
	    private readonly ConcurrentDictionary<string, PeerConnection> _peers = new ConcurrentDictionary<string, PeerConnection>();

	    public P2PNetwork(string peerUrl)
	    {
		    _peerUrl = peerUrl;
		    _networkClientFactory = new NetworkClientFactory();
	    }

		public async Task<string[]> ConnectPeerAsync(string webAddress)
	    {
		    //Remove client from peers in case it was reconnecting
			var result = _peers.Keys.
			    Where( p => string.Compare(p, webAddress, StringComparison.InvariantCultureIgnoreCase) != 0); 
		    await RegisterConnectionAsync(webAddress);
		    return result.ToArray();
	    }

	    public async Task<string[]> ConnectToPeerAsync(string webAddress)
	    {
		    var connection = await RegisterConnectionAsync(webAddress);
		    var peerConnections = await connection.Client.ConnectAsync(_peerUrl);
		    var connectedPeers = new List<string>();
		    foreach (var peerUrl in peerConnections)
		    {
			    if (await TryRegisterConnectionAsync(peerUrl))
					connectedPeers.Add(peerUrl);
			}

		    return connectedPeers.ToArray();
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

	    private async Task<bool> TryRegisterConnectionAsync(string peerUrl)
	    {
		    try
			{
				//Prevent looping to self
				if (string.Compare(peerUrl, _peerUrl, StringComparison.InvariantCultureIgnoreCase) != 0)
				{

					await RegisterConnectionAsync(peerUrl);
					return true;
				}

			}
		    catch (Exception) {/*ignore*/}
		    return false;
	    }

		private async Task<PeerConnection> RegisterConnectionAsync(string url)
	    {
		    var client = _networkClientFactory.Create(url);
		    await client.CheckAccessAsync();
		    var address = client.Host.AbsoluteUri;
		    var peerConnection = new PeerConnection(client);
		    if (_peers.TryAdd(address, peerConnection))
		    {
			    peerConnection.Accessible = true;
		    }

		    return peerConnection;
	    }
	}
}
