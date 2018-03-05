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

	    public async Task<string[]> RetrieveConnectionsAsync()
	    {
		    var response = await _httpClient.GetAsync(UriApi.Connections);
		    return await response.Content.ReadAsJsonAsync<string[]>();
	    }

		public async Task<string[]> ConnectAsync(string uri)
	    {
		    var response = await _httpClient.PostAsJsonAsync(UriApi.Connections, uri);
			return await response.Content.ReadAsJsonAsync<string[]>();
	    }

		public async Task DisconnectAsync(string uri)
	    {
		    await _httpClient.DeleteAsync(UriApi.Connections);
	    }

	    public async Task BroadcastAsync(TransactionSigned[] transactions)
	    {
		    await _httpClient.PostAsJsonAsync(UriApi.Connections, transactions);
	    }

	    public Uri Host => _httpClient.BaseAddress;
    }
}
