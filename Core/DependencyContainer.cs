using BlockChanPro.Core.Engine;
using BlockChanPro.Core.Engine.Data;
using BlockChanPro.Core.Engine.Network;

namespace BlockChanPro.Core
{
    public class DependencyContainer
    {
	    public readonly Cryptography Cryptography;
	    public readonly IP2PNetwork Network;
	    public readonly IChainData ChainData;
	    public readonly Engine.Engine Engine;

	    public DependencyContainer(string host, IFeedBack feedback)
	    {

		    Cryptography = new Cryptography();
			Network = new P2PNetwork(host);
			ChainData = new ChainData(feedback, Cryptography);
			Engine = new Engine.Engine(feedback, Network, ChainData);
	    }
}
}
