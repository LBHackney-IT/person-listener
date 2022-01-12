using AutoFixture;
using FluentAssertions;
using Hackney.Core.Http;
using Moq;
using PersonListener.Domain.Account;
using PersonListener.Gateway;
using PersonListener.Gateway.Interfaces;
using System;
using System.Threading.Tasks;
using Xunit;

namespace PersonListener.Tests.Gateway
{
    [Collection("LogCall collection")]
    public class AccountApiTests
    {
        private readonly Mock<IApiGateway> _mockApiGateway;

        private static readonly Guid _id = Guid.NewGuid();
        private static readonly Guid _correlationId = Guid.NewGuid();
        private const string AccountApiRoute = "https://some-domain.com/api";
        private const string AccountApiToken = "dksfghjskueygfakseygfaskjgfsdjkgfdkjsgfdkjgf";

        private const string ApiName = "Account";
        private const string AccountApiUrlKey = "AccountApiUrl";
        private const string AccountApiTokenKey = "AccountApiToken";

        public AccountApiTests()
        {
            _mockApiGateway = new Mock<IApiGateway>();

            _mockApiGateway.SetupGet(x => x.ApiName).Returns(ApiName);
            _mockApiGateway.SetupGet(x => x.ApiRoute).Returns(AccountApiRoute);
            _mockApiGateway.SetupGet(x => x.ApiToken).Returns(AccountApiToken);
        }

        private static string Route => $"{AccountApiRoute}/accounts/{_id}";


        [Fact]
        public void ConstructorTestInitialisesApiGateway()
        {
            new AccountApi(_mockApiGateway.Object);
            _mockApiGateway.Verify(x => x.Initialise(ApiName, AccountApiUrlKey, AccountApiTokenKey, null),
                                   Times.Once);
        }

        [Fact]
        public void GetAccountByIdAsyncExceptionThrown()
        {
            var exMessage = "This is an exception";
            _mockApiGateway.Setup(x => x.GetByIdAsync<AccountResponseObject>(Route, _id, _correlationId))
                           .ThrowsAsync(new Exception(exMessage));

            var sut = new AccountApi(_mockApiGateway.Object);
            Func<Task<AccountResponseObject>> func =
                async () => await sut.GetAccountByIdAsync(_id, _correlationId).ConfigureAwait(false);

            func.Should().ThrowAsync<Exception>().WithMessage(exMessage);
        }

        [Fact]
        public async Task GetAccountByIdAsyncNotFoundReturnsNull()
        {
            var sut = new AccountApi(_mockApiGateway.Object);
            var result = await sut.GetAccountByIdAsync(_id, _correlationId).ConfigureAwait(false);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAccountByIdAsyncCallReturnsAccount()
        {
            var account = new Fixture().Create<AccountResponseObject>();

            _mockApiGateway.Setup(x => x.GetByIdAsync<AccountResponseObject>(Route, _id, _correlationId))
                           .ReturnsAsync(account);

            var sut = new AccountApi(_mockApiGateway.Object);
            var result = await sut.GetAccountByIdAsync(_id, _correlationId).ConfigureAwait(false);

            result.Should().BeEquivalentTo(account);
        }
    }
}
