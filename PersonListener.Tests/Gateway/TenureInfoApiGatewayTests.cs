using AutoFixture;
using FluentAssertions;
using Hackney.Shared.Tenure.Boundary.Response;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using PersonListener.Gateway;
using PersonListener.Infrastructure.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PersonListener.Tests.Gateway
{
    [Collection("LogCall collection")]
    public class TenureInfoApiGatewayTests
    {
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly TenureInfoApiGateway _sut;
        private IConfiguration _configuration;
        private readonly static JsonSerializerOptions _jsonOptions = CreateJsonOptions();

        private static readonly Guid _correlationId = Guid.NewGuid();
        private const string TenureApiRoute = "https://some-domain.com/api/";
        private const string TenureApiToken = "dksfghjskueygfakseygfaskjgfsdjkgfdkjsgfdkjgf";

        public TenureInfoApiGatewayTests()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
                                  .Returns(_httpClient);

            var inMemorySettings = new Dictionary<string, string> {
                { "TenureApiUrl", TenureApiRoute },
                { "TenureApiToken", TenureApiToken }
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _sut = new TenureInfoApiGateway(_mockHttpClientFactory.Object, _configuration);
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

        private static string Route(Guid id) => $"{TenureApiRoute}tenures/{id}";

        private static bool ValidateRequest(string expectedRoute, HttpRequestMessage request)
        {
            var correlationIdHeader = request.Headers.GetValues("x-correlation-id")?.FirstOrDefault();
            return (request.RequestUri.ToString() == expectedRoute)
                && (request.Headers.Authorization.ToString() == TenureApiToken)
                && (correlationIdHeader == _correlationId.ToString());
        }

        private void SetupHttpClientResponse(string route, TenureResponseObject response)
        {
            HttpStatusCode statusCode = (response is null) ?
                HttpStatusCode.NotFound : HttpStatusCode.OK;
            HttpContent content = (response is null) ?
                null : new StringContent(JsonSerializer.Serialize(response, _jsonOptions));
            _mockHttpMessageHandler.Protected()
                   .Setup<Task<HttpResponseMessage>>("SendAsync",
                        ItExpr.Is<HttpRequestMessage>(y => ValidateRequest(route, y)),
                        ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(new HttpResponseMessage
                   {
                       StatusCode = statusCode,
                       Content = content,
                   });
        }

        private void SetupHttpClientErrorResponse(string route, string response)
        {
            HttpContent content = (response is null) ? null : new StringContent(response);
            _mockHttpMessageHandler.Protected()
                   .Setup<Task<HttpResponseMessage>>("SendAsync",
                        ItExpr.Is<HttpRequestMessage>(y => y.RequestUri.ToString() == route),
                        ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(new HttpResponseMessage
                   {
                       StatusCode = HttpStatusCode.InternalServerError,
                       Content = content,
                   });
        }

        private void SetupHttpClientException(string route, Exception ex)
        {
            _mockHttpMessageHandler.Protected()
                   .Setup<Task<HttpResponseMessage>>("SendAsync",
                        ItExpr.Is<HttpRequestMessage>(y => y.RequestUri.ToString() == route),
                        ItExpr.IsAny<CancellationToken>())
                   .ThrowsAsync(ex);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("sdrtgdfstg")]
        public void ConstructorTestInvalidRouteConfigThrows(string invalidValue)
        {
            var inMemorySettings = new Dictionary<string, string> {
                { "TenureApiUrl", invalidValue }
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            Action act = () => _ = new TenureInfoApiGateway(_mockHttpClientFactory.Object, _configuration);
            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ConstructorTestInvalidTokenConfigThrows(string invalidValue)
        {
            var inMemorySettings = new Dictionary<string, string> {
                { "TenureApiUrl", TenureApiRoute },
                { "TenureApiToken", invalidValue }
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            Action act = () => _ = new TenureInfoApiGateway(_mockHttpClientFactory.Object, _configuration);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GetTenureInfoByIdAsyncGetExceptionThrown()
        {
            var id = Guid.NewGuid();
            var exMessage = "This is an exception";
            SetupHttpClientException(Route(id), new Exception(exMessage));

            Func<Task<TenureResponseObject>> func =
                async () => await _sut.GetTenureInfoByIdAsync(id, _correlationId).ConfigureAwait(false);

            func.Should().ThrowAsync<Exception>().WithMessage(exMessage);
        }

        [Fact]
        public void GetTenureInfoByIdAsyncCallFailedExceptionThrown()
        {
            var id = Guid.NewGuid();
            var error = "This is an error message";
            SetupHttpClientErrorResponse(Route(id), error);

            Func<Task<TenureResponseObject>> func =
                async () => await _sut.GetTenureInfoByIdAsync(id, _correlationId).ConfigureAwait(false);

            func.Should().ThrowAsync<GetTenureException>()
                         .WithMessage($"Failed to get person details for id {id}. " +
                         $"Status code: {HttpStatusCode.InternalServerError}; Message: {error}");
        }

        [Fact]
        public async Task GetTenureInfoByIdAsyncNotFoundReturnsNull()
        {
            var id = Guid.NewGuid();
            SetupHttpClientResponse(Route(id), null);

            var result = await _sut.GetTenureInfoByIdAsync(id, _correlationId).ConfigureAwait(false);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetTenureInfoByIdAsyncCallReturnsPerson()
        {
            var id = Guid.NewGuid();
            var person = new Fixture().Create<TenureResponseObject>();
            SetupHttpClientResponse(Route(id), person);

            var result = await _sut.GetTenureInfoByIdAsync(id, _correlationId).ConfigureAwait(false);

            result.Should().BeEquivalentTo(person);
        }
    }
}
