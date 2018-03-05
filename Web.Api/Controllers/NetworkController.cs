using System.Threading.Tasks;
using BlockChanPro.Core.Engine.Network;
using BlockChanPro.Model.Contracts;
using BlockChanPro.Model.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BlockChanPro.Web.Api.Controllers
{
    [Route("")]
    public class NetworkController : Controller, INetworkApi
	{
	    private readonly IP2PNetwork _netwrok;

	    public NetworkController(IP2PNetwork netwrok)
	    {
		    _netwrok = netwrok;
	    }

	    [HttpGet(UriApi.Connections)]
        public Task<string[]> RetrieveConnectionsAsync()
		{
	        return Task.FromResult(_netwrok.GetConnections());
        }

        [HttpPost(UriApi.Connections)]
        public Task<string[]> ConnectAsync([FromBody]string address)
        {
	        return Task.FromResult(_netwrok.Connect(address));
        }

        [HttpDelete(UriApi.Connections)]
        public Task DisconnectAsync(string uri)
        {
	        throw new System.NotImplementedException();
	        //return Task.CompletedTask;
        }

		[HttpPost(UriApi.Transactions)]
		public Task BroadcastAsync(TransactionSigned[] transactions)
		{
			throw new System.NotImplementedException();
		}
	}
}
