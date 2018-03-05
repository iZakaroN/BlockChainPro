using System;

namespace BlockChanPro.Model.Interfaces
{
    public interface INetworkClient : INetworkApi
    {
	    Uri Host { get; }
    }
}
