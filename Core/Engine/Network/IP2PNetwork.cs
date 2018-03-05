using BlockChanPro.Model.Contracts;

namespace BlockChanPro.Core.Engine.Network
{
	public interface IP2PNetwork
	{
		/// <summary>
		/// Connect to the node using <see cref="webAddress"/>
		/// </summary>
		/// <param name="webAddress">Web address that can be used to connect to the node</param>
		/// <returns></returns>
		string[] Connect(string webAddress);

		/// <summary>
		/// Retrieve available connections
		/// </summary>
		/// <returns></returns>
		string[] GetConnections();

		/// <summary>
		/// Broadcast a new mined block
		/// </summary>
		/// <param name="block"></param>
		/// <param name="peerUri"></param>
		void Broadcast(BlockHashed block, string peerUri = null);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="transactions"></param>
		/// <param name="peerUri">peer that broadcast</param>
		void Broadcast(TransactionSigned[] transactions, string peerUri = null);
	}
}