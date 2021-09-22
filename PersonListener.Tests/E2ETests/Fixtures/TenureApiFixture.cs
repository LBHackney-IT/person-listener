using PersonListener.Domain.TenureInformation;
using AutoFixture;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PersonListener.Tests.E2ETests.Fixtures
{
    public class TenureApiFixture : IDisposable
    {
        private readonly Fixture _fixture = new Fixture();
        private readonly JsonSerializerOptions _jsonOptions;
        private static HttpListener _httpListener;
        public static TenureResponseObject TenureResponse { get; private set; }

        public static string TenureApiRoute => "http://localhost:5678/api/v1/";
        public static string TenureApiToken => "sdjkhfgsdkjfgsdjfgh";

        public TenureApiFixture()
        {
            _jsonOptions = CreateJsonOptions();
            StartTenureApiStub();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool _disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                if (_httpListener.IsListening)
                    _httpListener.Stop();
                TenureResponse = null;

                _disposed = true;
            }
        }

        private JsonSerializerOptions CreateJsonOptions()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }

        private void StartTenureApiStub()
        {
            Environment.SetEnvironmentVariable("TenureApiUrl", TenureApiRoute);
            Environment.SetEnvironmentVariable("TenureApiToken", TenureApiToken);
            Task.Run(() =>
            {
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add(TenureApiRoute);
                _httpListener.Start();

                // GetContext method blocks while waiting for a request. 
                HttpListenerContext context = _httpListener.GetContext();
                HttpListenerResponse response = context.Response;

                if (context.Request.Headers["Authorization"] != TenureApiToken)
                {
                    response.StatusCode = (int) HttpStatusCode.Unauthorized;
                }
                else
                {
                    response.StatusCode = (int) ((TenureResponse is null) ? HttpStatusCode.NotFound : HttpStatusCode.OK);
                    string responseBody = string.Empty;
                    if (TenureResponse is null)
                    {
                        responseBody = context.Request.Url.Segments.Last();
                    }
                    else
                    {
                        responseBody = JsonSerializer.Serialize(TenureResponse, _jsonOptions);
                    }
                    Stream stream = response.OutputStream;
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write(responseBody);
                        writer.Close();
                    }
                }
            });
        }

        public void GivenTheTenureHasNoHouseholdMembers(TenureResponseObject tenureResponse)
        {
            tenureResponse.HouseholdMembers.Clear();
        }

        public void GivenTheTenureDoesNotExist(Guid id)
        {
            // Nothing to do here
        }

        public TenureResponseObject GivenTheTenureExists(Guid id)
        {
            TenureResponse = _fixture.Build<TenureResponseObject>()
                                      .With(x => x.Id, id)
                                      .With(x => x.HouseholdMembers,
                                                 _fixture.Build<HouseholdMembers>()
                                                         .With(y => y.PersonTenureType, "Occupant")
                                                         .CreateMany(3)
                                                         .ToList())
                                      .Create();
            return TenureResponse;
        }
    }
}
