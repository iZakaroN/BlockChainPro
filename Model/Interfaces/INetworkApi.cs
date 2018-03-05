using System.Threading.Tasks;
using BlockChanPro.Model.Contracts;

namespace BlockChanPro.Model.Interfaces
{
	public interface INetworkApi
	{
		Task<string[]> RetrieveConnectionsAsync();
		Task<string[]> ConnectAsync(string uri);
		Task DisconnectAsync(string uri);
		Task BroadcastAsync(TransactionSigned[] transactions);
	}
}