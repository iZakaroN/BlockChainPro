using BlockChanPro.Model.Interfaces;
using Web.Shared;

namespace BlockChanPro.Core.Engine.Network
{
	public class PeerConnection
	{
		public INetworkApi Client { get; }
		public bool Accessible = false;

		public PeerConnection(INetworkApi peerClient)
		{
			Client = peerClient;
		}

	}
}