using System.Threading.Tasks;
using BlockChanPro.Core.Engine;
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
		private readonly IEngine _engine;

		public NetworkController(IP2PNetwork netwrok, IEngine engine)
		{
			_netwrok = netwrok;
			_engine = engine;
		}

		[HttpGet(ApiConstants.Connections)]
		public Task<string> GetVersionAsync()
		{
			return Task.FromResult(ApiConstants.Version);
		}

	    [HttpGet(ApiConstants.Connections)]
		public Task<string[]> GetConnectionsAsync()
		{
			return _netwrok.GetConnectionsAsync();
		}

		[HttpPost(ApiConstants.Connections)]
        public Task<string[]> ConnectAsync([FromBody]string address)
        {
	        return _netwrok.ConnectAsync(address);
        }

		[HttpPost(ApiConstants.Transactions)]
		public Task BroadcastAsync([FromBody]TransactionsBundle transactions)
		{
			return _engine.AcceptTransactionsAsync(transactions);
		}

		public Task BroadcastAsync([FromBody]BlockBundle block)
		{
			return _engine.AcceptBlockAsync(block);
		}
	}
}
