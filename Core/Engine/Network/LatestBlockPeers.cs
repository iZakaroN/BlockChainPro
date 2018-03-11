using System;
using System.Collections.Generic;
using System.Text;
using BlockChanPro.Model.Contracts;

namespace BlockChanPro.Core.Engine.Network
{
    public class LatestBlockPeers
    {
	    public BlockIdentity Block { get; }
	    public PeerConnection[] Connections { get; }

	    public LatestBlockPeers(BlockIdentity block, PeerConnection[] connections)
	    {
		    Block = block;
		    Connections = connections;
	    }

    }
}
