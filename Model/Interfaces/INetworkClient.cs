using System;
using System.Threading.Tasks;

namespace BlockChanPro.Model.Interfaces
{
    public interface INetworkClient : INetworkApi
    {
	    Uri Host { get; }

	    Task CheckAccessAsync();
    }
}
