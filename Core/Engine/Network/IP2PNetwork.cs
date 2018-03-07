using System.Threading.Tasks;
using BlockChanPro.Model.Contracts;

namespace BlockChanPro.Core.Engine.Network
{
	public interface IP2PNetwork
	{
		Task<string[]> ConnectAsync(string webAddress);

		Task<string[]> GetConnectionsAsync();

		Task BroadcastAsync(TransactionSigned[] transactions);

		Task Broadcast(BlockHashed block);
	}
}