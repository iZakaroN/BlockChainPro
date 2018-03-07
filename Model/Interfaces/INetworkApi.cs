using System.Threading.Tasks;
using BlockChanPro.Model.Contracts;

namespace BlockChanPro.Model.Interfaces
{
	public interface INetworkApi
	{
		/// <summary>
		/// Retrieve the version of the api
		/// </summary>
		/// <returns></returns>
		Task<string> GetVersionAsync();

		/// <summary>
		/// Retrieve available connections
		/// </summary>
		/// <returns></returns>
		Task<string[]> GetConnectionsAsync();

		/// <summary>
		/// Connect to the node using <see cref="webAddress"/>
		/// </summary>
		/// <param name="webAddress">Web address that can be used to connect to the node</param>
		/// <returns></returns>
		Task<string[]> ConnectAsync(string webAddress);

		/// <summary>
		/// Broadcast a pending transactions
		/// </summary>
		/// <param name="transactions"></param>
		Task BroadcastAsync(TransactionsBundle transactions);

		/// <summary>
		/// Broadcast a new mined block
		/// </summary>
		/// <param name="block"></param>
		Task BroadcastAsync(BlockBundle block);
	}
}