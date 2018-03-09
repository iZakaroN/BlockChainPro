using System.IO;
using System.Threading.Tasks;
using BlockChanPro.Core.Engine;
using BlockChanPro.Core.Engine.Network;
using BlockChanPro.Model.Contracts;
using BlockChanPro.Model.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

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

		[HttpGet(ApiConstants.Root)]
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
			//TODO: Retrieve ip from request, leave only port as parameter. Remove Sender from bundles
			return _netwrok.ConnectPeerAsync(address);
        }

		[HttpPost(ApiConstants.Transactions)]
		public Task BroadcastAsync([FromBody]TransactionsBundle transactions)
		{
			//var remoteIpAddress = httpContext.GetFeature<IHttpConnectionFeature>()?.RemoteIpAddress
			return _engine.AcceptTransactionsAsync(transactions);
		}

		//[HttpPost(ApiConstants.Blocks)]
		public async Task BroadcastAsync([FromBody]BlockBundle block)
		{
			await _engine.AcceptBlockAsync(block);
		}

		[HttpPost(ApiConstants.Blocks)]
		public async Task BroadcastAsync()
		{
			//var block = JsonConvert.DeserializeObject<BlockBundle>(data);
			var block = await ReadAsJsonAsync<BlockBundle>(Request);
			await _engine.AcceptBlockAsync(block);
		}

		public static async Task<T> ReadAsJsonAsync<T>(HttpRequest request)
		{
			using (var stream = new StreamReader(request.Body))
			{
				var body = await stream.ReadToEndAsync();
				return JsonConvert.DeserializeObject<T>(body);
			}
		}


	}
}
