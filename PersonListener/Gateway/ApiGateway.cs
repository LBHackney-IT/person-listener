using Microsoft.Extensions.Configuration;
using PersonListener.Gateway.Interfaces;
using PersonListener.Infrastructure.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PersonListener.Gateway
{
    // TODO - Move somewhere common (Hackney.Core.Http ?)

    public class ApiGateway : IApiGateway
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly static JsonSerializerOptions _jsonOptions = CreateJsonOptions();

        public string ApiRoute { get; private set; }
        public string ApiToken { get; private set; }
        public string ApiName { get; private set; }
        public Dictionary<string, string> RequestHeaders { get; private set; }

        private bool _initialised = false;

        public ApiGateway(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        protected static JsonSerializerOptions CreateJsonOptions()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }

        public void Initialise(string apiName, string configKeyApiUrl, string configKeyApiToken, Dictionary<string, string> headers = null)
        {
            if (string.IsNullOrEmpty(apiName)) throw new ArgumentNullException(nameof(apiName));
            ApiName = apiName;

            var apiRoute = _configuration.GetValue<string>(configKeyApiUrl)?.TrimEnd('/');
            if (string.IsNullOrEmpty(apiRoute) || !Uri.IsWellFormedUriString(apiRoute, UriKind.Absolute))
                throw new ArgumentException($"Configuration does not contain a setting value for the key {configKeyApiUrl}.");
            ApiRoute = apiRoute;

            var apiToken = _configuration.GetValue<string>(configKeyApiToken);
            if (string.IsNullOrEmpty(apiToken))
                throw new ArgumentException($"Configuration does not contain a setting value for the key {configKeyApiToken}.");
            ApiToken = apiToken;

            RequestHeaders = headers ?? new Dictionary<string, string>();

            _initialised = true;
        }

        public async Task<T> GetByIdAsync<T>(string route, Guid id, Guid correlationId) where T : class
        {
            if (!_initialised) throw new InvalidOperationException("Initialise() must be called before any other calls are made");

            var client = _httpClientFactory.CreateClient();

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-correlation-id", correlationId.ToString());
            client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(ApiToken);
            foreach (var pair in RequestHeaders)
                client.DefaultRequestHeaders.Add(pair.Key, pair.Value);

            var response = await client.GetAsync(new Uri(route))
                                       .ConfigureAwait(false);

            if (response.StatusCode is HttpStatusCode.NotFound)
                return null;

            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
                return JsonSerializer.Deserialize<T>(responseBody, _jsonOptions);

            throw new GetFromApiException(ApiName, route, client.DefaultRequestHeaders.ToList(),
                                          id, response.StatusCode, responseBody);
        }
    }
}
