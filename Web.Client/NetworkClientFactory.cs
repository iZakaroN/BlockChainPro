using System;
using System.Net.Http;
using System.Net.Http.Headers;
using BlockChanPro.Model.Interfaces;
using BlockChanPro.Web.Client;

namespace Web.Shared
{
	public interface INetworkClientFactory
	{
		INetworkClient Create(string url);
	}
    public class NetworkClientFactory : INetworkClientFactory
	{
		public INetworkClient Create(string url)
		{
			if (!url.TryParseUrl(out var uri))
				throw new ArgumentException("Not valid web address");

			var httpClient = new HttpClient {BaseAddress = uri};
			httpClient.DefaultRequestHeaders.Accept.Clear();
			httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			return new NetworkClient(httpClient);
		}
	}
}
