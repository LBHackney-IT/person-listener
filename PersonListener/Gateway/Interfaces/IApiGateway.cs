using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PersonListener.Gateway.Interfaces
{
    // TODO - Move somewhere common (Hackney.Core.Http ?)

    public interface IApiGateway
    {
        string ApiRoute { get; }
        string ApiToken { get; }
        string ApiName { get; }
        Dictionary<string, string> RequestHeaders { get; }

        void Initialise(string apiName, string configKeyApiUrl, string configKeyApiToken, Dictionary<string, string> headers = null);
        Task<T> GetByIdAsync<T>(string route, Guid id, Guid correlationId) where T : class;
    }
}
