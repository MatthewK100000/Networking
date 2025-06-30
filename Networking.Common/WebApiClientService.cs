#pragma warning disable IDE0290 // avoiding new c#12 feature using primary constructor syntax
#pragma warning disable IDE0090 // 'new' expression can be simplified

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Http;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;

namespace Networking.Common;


public sealed class WebApiClientService {

    public IHttpClientFactory _httpClientFactory;
    public ILogger<WebApiClientService> _logger;

    public WebApiClientService(IHttpClientFactory httpClientFactory,
                        ILogger<WebApiClientService> logger
                        ) 
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }


    public async Task<string?> Post(string endpoint, JObject bodyContent) {
        // Create the client
        HttpClient client = _httpClientFactory.CreateClient();

        using StringContent httpContent = new StringContent(
                                            bodyContent.ToString(),
                                            Encoding.UTF8,
                                            "application/json"
                                            );



        HttpResponseMessage response = await client.PostAsync(
                requestUri: endpoint,
                httpContent
        );

        string responseContent = null!;

        try {
            response.EnsureSuccessStatusCode();
            responseContent = await response.Content.ReadAsStringAsync();

        }
        finally {
            response.Dispose();
        }

        return responseContent;
    }


    public async Task<string> Get(string endpoint) {
        // Create the client
        HttpClient client = _httpClientFactory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
                requestUri: endpoint
        );

        string responseContent = null!;

        try {
            response.EnsureSuccessStatusCode();
            responseContent = await response.Content.ReadAsStringAsync();
        }
        finally {
            response.Dispose();
        }

        return responseContent;
    }






    

}
