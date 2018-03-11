using System.Threading.Tasks;
using BlockChanPro.Core.Engine.Data;
using BlockChanPro.Model.Contracts;

namespace BlockChanPro.Core.Engine.Network
{
	public interface IP2PNetwork
	{
		/// <summary>
		/// Connect a peer to your network and return already known peers
		/// </summary>
		/// <param name="webAddress">Peer web address</param>
		/// <returns>Known peers</returns>
		Task<string[]> ConnectPeerAsync(string webAddress);


		/// <summary>
		/// Connect to specified peer address and try discover other accessible nodes
		/// </summary>
		/// <param name="webAddress">Peer web address</param>
		/// <returns>Number of connected peers</returns>
		Task<int> ConnectToPeerAsync(string webAddress);

		Task<string[]> GetConnectionsAsync();

		Task BroadcastAsync(TransactionSigned[] transactions);

		Task BroadcastAsync(BlockHashed block);

		Task BlockchainSync(IChainData chainData);
	}
}