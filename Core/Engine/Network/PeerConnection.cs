using System.Threading.Tasks;
using BlockChanPro.Model.Interfaces;

namespace BlockChanPro.Core.Engine.Network
{
	public class PeerConnection
	{
		public INetworkClient Client { get; }
		public bool Accessible = false;

		public PeerConnection(INetworkClient peerClient)
		{
			Client = peerClient;
		}

		public Task DisconnectAsync()
		{
			return Client.DisposeAsync();
		}
	}
}