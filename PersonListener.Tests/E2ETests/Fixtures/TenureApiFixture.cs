using PersonListener.Domain.TenureInformation;
using AutoFixture;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;
using PersonListener.Boundary;
using Force.DeepCloner;
using PersonListener.Domain;
using PersonListener.Infrastructure;

namespace PersonListener.Tests.E2ETests.Fixtures
{
    public class TenureApiFixture : IDisposable
    {
        private readonly Fixture _fixture = new Fixture();
        private readonly JsonSerializerOptions _jsonOptions;
        private static HttpListener _httpListener;
        public static TenureResponseObject TenureResponse { get; private set; }

        public Tenure Tenure { get; private set; }

        public List<string> ReceivedCorrelationIds { get; private set; } = new List<string>();

        public static string TenureApiRoute => "http://localhost:5678/api/v1/";
        public static string TenureApiToken => "sdjkhfgsdkjfgsdjfgh";

        public Guid RemovedPersonId { get; private set; }

        public EventData MessageEventData { get; private set; }

        public List<TenureResponseObject> TenureResponses { get; private set; } = new List<TenureResponseObject>();



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
            ReceivedCorrelationIds.Clear();

            Task.Run(() =>
            {
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add(TenureApiRoute);
                _httpListener.Start();

                // GetContext method blocks while waiting for a request.
                while (true)
                {
                    HttpListenerContext context = _httpListener.GetContext();
                    HttpListenerResponse response = context.Response;

                    if (context.Request.Headers["Authorization"] != TenureApiToken)
                    {
                        response.StatusCode = (int) HttpStatusCode.Unauthorized;
                    }
                    else
                    {
                        ReceivedCorrelationIds.Add(context.Request.Headers["x-correlation-id"]);
                        var thisResponse = TenureResponse;
                        if (TenureResponses.Any())
                        {
                            var requestedId = context.Request.Url.Segments.Last();
                            thisResponse = TenureResponses.FirstOrDefault(x => x.Id.ToString() == requestedId);
                        }

                        response.StatusCode = (int) ((thisResponse is null) ? HttpStatusCode.NotFound : HttpStatusCode.OK);
                        string responseBody = string.Empty;
                        if (thisResponse is null)
                        {
                            responseBody = context.Request.Url.Segments.Last();
                        }
                        else
                        {
                            responseBody = JsonSerializer.Serialize(thisResponse, _jsonOptions);
                        }
                        Stream stream = response.OutputStream;
                        using (var writer = new StreamWriter(stream))
                        {
                            writer.Write(responseBody);
                            writer.Close();
                        }
                    }
                }
            });
        }


        public void GivenThePersonTenuresExist(PersonDbEntity dbEntity)
        {
            for (int i = 0; i < dbEntity.Tenures.Count; i++)
            {
                var personTenure = dbEntity.Tenures[i];
                var personType = dbEntity.PersonTypes[i];
                var Tenure = CreateTenureForPerson(personTenure.Id, dbEntity.Id, personType);
                TenureResponses.Add(Tenure);
            }
        }

        private TenureResponseObject CreateTenureForPerson(Guid tenureId, Guid personId, PersonType personType)
        {
            TenureType tt;
            bool isResponsible;
            switch (personType)
            {
                case PersonType.HouseholdMember:
                    tt = TenureTypes.Secure;
                    isResponsible = false;
                    break;
                case PersonType.Freeholder:
                    tt = TenureTypes.Freehold;
                    isResponsible = true;
                    break;
                default:
                    tt = TenureTypes.Secure;
                    isResponsible = true;
                    break;
            }
            var hms = _fixture.Build<HouseholdMembers>()
                              .With(x => x.DateOfBirth, DateTime.UtcNow.AddYears(-40))
                              .With(x => x.PersonTenureType, Enum.GetName(typeof(PersonType), personType))
                              .With(x => x.IsResponsible, isResponsible)
                              .CreateMany(3).ToList();
            hms.Last().Id = personId;

            return _fixture.Build<TenureResponseObject>()
                           .With(x => x.Id, tenureId)
                           .With(x => x.TenureType, tt)
                           .With(x => x.HouseholdMembers, hms)
                           .Create();
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
        private List<HouseholdMembers> CreateHouseholdMembers(int count = 3)
        {
            return _fixture.Build<HouseholdMembers>()
                           .With(x => x.Id, () => Guid.NewGuid())
                           .With(x => x.DateOfBirth, DateTime.UtcNow.AddYears(-40))
                           .With(x => x.PersonTenureType, "Tenant")
                           .CreateMany(count).ToList();
        }

        private void CreateMessageEventDataForPersonRemoved(Guid id)
        {
            var oldData = CreateHouseholdMembers();
            var newData = oldData.DeepClone();

            var removedHm = CreateHouseholdMembers(1).First();
            removedHm.Id = id;
            oldData.Add(removedHm);
            RemovedPersonId = id;

            MessageEventData = new EventData()
            {
                OldData = new Dictionary<string, object> { { "householdMembers", oldData } },
                NewData = new Dictionary<string, object> { { "householdMembers", newData } }
            };
        }

        public void GivenAPersonWasRemoved(Guid id)
        {
            CreateMessageEventDataForPersonRemoved(id);
        }
    }
}
