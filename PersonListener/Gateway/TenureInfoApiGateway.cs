using Hackney.Core.Logging;
using Microsoft.Extensions.Configuration;
using PersonListener.Domain.TenureInformation;
using PersonListener.Gateway.Interfaces;
using PersonListener.Infrastructure.Exceptions;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PersonListener.Gateway
{
    public class TenureInfoApiGateway : ITenureInfoApiGateway
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _getTenureApiRoute;
        private readonly string _getTenureApiToken;

        private const string TenureApiUrl = "TenureApiUrl";
        private const string TenureApiToken = "TenureApiToken";
        private readonly static JsonSerializerOptions _jsonOptions = CreateJsonOptions();

        public TenureInfoApiGateway(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _getTenureApiRoute = configuration.GetValue<string>(TenureApiUrl)?.TrimEnd('/');
            if (string.IsNullOrEmpty(_getTenureApiRoute) || !Uri.IsWellFormedUriString(_getTenureApiRoute, UriKind.Absolute))
                throw new ArgumentException($"Configuration does not contain a valid setting value for the key {TenureApiUrl}.");

            _getTenureApiToken = configuration.GetValue<string>(TenureApiToken);
            if (string.IsNullOrEmpty(_getTenureApiToken))
                throw new ArgumentException($"Configuration does not contain a setting value for the key {TenureApiToken}.");
        }

        private static JsonSerializerOptions CreateJsonOptions()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }

        [LogCall]
        public async Task<TenureResponseObject> GetTenureInfoByIdAsync(Guid id)
        {
            var client = _httpClientFactory.CreateClient();
            var getTenureRoute = $"{_getTenureApiRoute}/tenures/{id}";

            client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(_getTenureApiToken);
            var response = await client.GetAsync(new Uri(getTenureRoute))
                                       .ConfigureAwait(false);

            if (response.StatusCode is HttpStatusCode.NotFound)
                return null;

            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
                return JsonSerializer.Deserialize<TenureResponseObject>(responseBody, _jsonOptions);

            throw new GetTenureException(id, response.StatusCode, responseBody);
        }
    }
}
