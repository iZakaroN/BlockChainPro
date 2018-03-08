using System;
using System.Net.Http;
using System.Threading.Tasks;
using BlockChanPro.Model.Contracts;
using BlockChanPro.Model.Interfaces;

namespace Web.Shared
{
    public class NetworkClient : INetworkClient
    {
	    private readonly HttpClient _httpClient;

	    public NetworkClient(HttpClient httpClient)
	    {
		    _httpClient = httpClient;
	    }

	    public async Task<string> GetVersionAsync()
	    {
		    var response = await _httpClient.GetAsync(ApiConstants.Root);
		    response.EnsureSuccessStatusCode();
		    return await response.Content.ReadAsJsonAsync<string>();
	    }

		public async Task<string[]> GetConnectionsAsync()
	    {
		    var response = await _httpClient.GetAsync(ApiConstants.Connections);
		    response.EnsureSuccessStatusCode();
		    return await response.Content.ReadAsJsonAsync<string[]>();
	    }

		public async Task<string[]> ConnectAsync(string senderUri)
	    {
		    var response = await _httpClient.PostAsJsonAsync(ApiConstants.Connections, senderUri);
		    response.EnsureSuccessStatusCode();
			return await response.Content.ReadAsJsonAsync<string[]>();
	    }

	    public Task BroadcastAsync(TransactionsBundle transactions)
	    {
		    return _httpClient.PostAsJsonAsync(ApiConstants.Transactions, transactions);
	    }

	    public Task BroadcastAsync(BlockBundle block)
	    {
		    return _httpClient.PostAsJsonAsync(ApiConstants.Blocks, block);
	    }

		public Uri Host => _httpClient.BaseAddress;

	    public async Task CheckAccessAsync()
	    {
		    var version = await GetVersionAsync();
			if (version != ApiConstants.Version)
			    throw new ApiException($"Peer has invalid api version 'v{version}'. Expected '{ApiConstants.Version}'");
	    }
	}
}
