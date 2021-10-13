using Hackney.Core.Logging;
using Hackney.Shared.Tenure.Boundary.Response;
using Microsoft.Extensions.Configuration;
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
        private const string ApiName = "Tenure";
        private const string TenureApiUrl = "TenureApiUrl";
        private const string TenureApiToken = "TenureApiToken";

        private readonly IApiGateway _apiGateway;

        public TenureInfoApiGateway(IApiGateway apiGateway)
        {
            _apiGateway = apiGateway;
            _apiGateway.Initialise(ApiName, TenureApiUrl, TenureApiToken);
        }

        [LogCall]
        public async Task<TenureResponseObject> GetTenureInfoByIdAsync(Guid id, Guid correlationId)
        {
            var route = $"{_apiGateway.ApiRoute}/tenures/{id}";
            return await _apiGateway.GetByIdAsync<TenureResponseObject>(route, id, correlationId);
        }
    }
}
