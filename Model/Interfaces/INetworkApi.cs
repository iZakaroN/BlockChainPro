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

		/// <summary>
		/// Retrieve page of sequential blocks starting from <see cref="start"/>
		/// </summary>
		/// <param name="start">First block height to retrieve. Default: {last block}</param>
		/// <param name="count">Maximum number of blocks to retrieve. Default: {1}</param>
		/// <returns>Available blocks</returns>
		Task<BlockHashed[]> GetBlocksAsync(int? start = null, int? count = null);

		/// <summary>
		/// Retrieve exact list of blocks/>
		/// </summary>
		/// <param name="indexes">Block indexes to retrieve</param>
		/// <returns>Available blocks</returns>
		Task<BlockHashed[]> GetBlocksAsync(int[] indexes);

		/// <summary>
		/// Retrieve page of sequential block identities starting from <see cref="start"/>
		/// </summary>
		/// <param name="start">First block height to retrieve. Default: {last block}</param>
		/// <param name="count">Maximum number of blocks to retrieve. Default: {1}</param>
		/// <returns>Available blocks</returns>
		Task<BlockIdentity[]> GetBlockIdentitiesAsync(int? start = null, int? count = null);

		/// <summary>
		/// Retrieve exact list of block identities/>
		/// </summary>
		/// <param name="blockIndexes">Block indexes to retrieve</param>
		/// <returns>Available blocks</returns>
		Task<BlockIdentity[]> GetBlockIdentitiesAsync(int[] blockIndexes);

	}
}