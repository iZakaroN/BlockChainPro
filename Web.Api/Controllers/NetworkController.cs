using System;
using System.IO;
using System.Threading.Tasks;
using BlockChanPro.Core.Engine;
using BlockChanPro.Core.Engine.Data;
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
		private readonly IChainData _chainData;

		public NetworkController(IP2PNetwork netwrok, IEngine engine, IChainData chainData)
		{
			_netwrok = netwrok;
			_engine = engine;
			_chainData = chainData;
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

		[HttpPost(ApiConstants.Block)]
		public async Task BroadcastAsync()
		{
			//var block = JsonConvert.DeserializeObject<BlockBundle>(data);
			var block = await ReadAsJsonAsync<BlockBundle>(Request);
			await _engine.AcceptBlockAsync(block);
		}

		/// <summary>
		/// Retrieve blocks
		/// </summary>
		/// <param name="s">startBlockIndex</param>
		/// <param name="c">blocksCount</param>
		/// <param name="i">blockIndexes</param>
		/// <returns>Available blocks</returns>
		[HttpGet(ApiConstants.Block)]
		public Task<BlockHashed[]> GetBlocksAsync([FromQuery]int? s, [FromQuery]int? c, [FromQuery]int[] i)
		{
			if (i != null && i.Length > 0)
			{
				if (s != null || c != null)
					throw new ArgumentException("'index'(i) parameter is not compatible with 'start'(s) and 'count'(c)");
				return GetBlocksAsync(i);
			}
			return GetBlocksAsync(s, c);
		}

		/// <summary>
		/// Retrieve block identities
		/// </summary>
		/// <param name="s">startBlockIndex</param>
		/// <param name="c">blocksCount</param>
		/// <param name="i">blockIndexes</param>
		/// <returns>Available blocks</returns>
		[HttpGet(ApiConstants.BlockIdentity)]
		public Task<BlockIdentity[]> GetBlockIdentitiesAsync([FromQuery]int? s, [FromQuery]int? c, [FromQuery]int[] i)
		{
			if (i != null && i.Length > 0)
			{
				if (s != null || c != null)
					throw new ArgumentException("'index'(i) parameter is not compatible with 'start'(s) and 'count'(c)");
				return GetBlockIdentitiesAsync(i);
			}
			return GetBlockIdentitiesAsync(s, c);
		}

		public Task<BlockHashed[]> GetBlocksAsync(int? start = null, int? count = null)
		{
			return _chainData.GetBlocksAsync(start ?? -1, count ?? 1);
		}

		public Task<BlockHashed[]> GetBlocksAsync(int[] indexes)
		{
			return _chainData.GetBlocksAsync(indexes);
		}

		public Task<BlockIdentity[]> GetBlockIdentitiesAsync(int? start = null, int? count = null)
		{
			return _chainData.GetBlockIdentitiesAsync(start ?? -1, count ?? 1);
		}

		public Task<BlockIdentity[]> GetBlockIdentitiesAsync(int[] indexes)
		{
			return _chainData.GetBlockIdentitiesAsync(indexes);
		}


		private static async Task<T> ReadAsJsonAsync<T>(HttpRequest request)
		{
			using (var stream = new StreamReader(request.Body))
			{
				var body = await stream.ReadToEndAsync();
				return JsonConvert.DeserializeObject<T>(body);
			}
		}


	}
}
