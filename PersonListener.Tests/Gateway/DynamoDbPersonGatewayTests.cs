using Amazon.DynamoDBv2.DataModel;
using AutoFixture;
using FluentAssertions;
using Hackney.Core.Testing.DynamoDb;
using Hackney.Core.Testing.Shared;
using Hackney.Shared.Person;
using Hackney.Shared.Person.Factories;
using Hackney.Shared.Person.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using PersonListener.Gateway;
using System;
using System.Threading.Tasks;
using Xunit;

namespace PersonListener.Tests.Gateway
{
    [Collection("AppTest collection")]
    public class DynamoDbPersonGatewayTests : IDisposable
    {
        private readonly Fixture _fixture = new Fixture();
        private readonly Mock<ILogger<DynamoDbPersonGateway>> _logger;
        private readonly DynamoDbPersonGateway _classUnderTest;
        private readonly IDynamoDbFixture _dbFixture;
        private IDynamoDBContext DynamoDb => _dbFixture.DynamoDbContext;

        public DynamoDbPersonGatewayTests(MockApplicationFactory appFactory)
        {
            _dbFixture = appFactory.DynamoDbFixture;
            _logger = new Mock<ILogger<DynamoDbPersonGateway>>();
            _classUnderTest = new DynamoDbPersonGateway(DynamoDb, _logger.Object);
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
                _disposed = true;
            }
        }

        private async Task InsertDatatoDynamoDB(Person entity)
        {
            await _dbFixture.SaveEntityAsync(entity.ToDatabase()).ConfigureAwait(false);
        }

        private Person ConstructPerson()
        {
            var entity = _fixture.Build<Person>()
                                 .With(x => x.DateOfBirth, DateTime.UtcNow.AddYears(-35))
                                 .With(x => x.LastModified, DateTime.UtcNow.AddHours(-1))
                                 .With(x => x.VersionNumber, (int?) null)
                                 .Create();
            return entity;
        }

        [Fact]
        public async Task GetPersonByIdAsyncTestReturnsRecord()
        {
            var domainEntity = ConstructPerson();
            await InsertDatatoDynamoDB(domainEntity).ConfigureAwait(false);

            var result = await _classUnderTest.GetPersonByIdAsync(domainEntity.Id).ConfigureAwait(false);

            result.Should().BeEquivalentTo(domainEntity, (e) => e.Excluding(y => y.VersionNumber));
            result.VersionNumber.Should().Be(0);

            _logger.VerifyExact(LogLevel.Debug, $"Calling IDynamoDBContext.LoadAsync for id {domainEntity.Id}", Times.Once());
        }

        [Fact]
        public async Task GetPersonByIdAsyncTestReturnsNullWhenNotFound()
        {
            var id = Guid.NewGuid();
            var result = await _classUnderTest.GetPersonByIdAsync(id).ConfigureAwait(false);

            result.Should().BeNull();

            _logger.VerifyExact(LogLevel.Debug, $"Calling IDynamoDBContext.LoadAsync for id {id}", Times.Once());
        }

        [Fact]
        public async Task SavePersonAsyncTestUpdatesDatabase()
        {
            var person = ConstructPerson();
            await InsertDatatoDynamoDB(person).ConfigureAwait(false);

            person.FirstName = "New name";
            person.Surname = "New surname";
            person.PlaceOfBirth = "New place of birth";
            person.VersionNumber = 0;
            await _classUnderTest.SavePersonAsync(person).ConfigureAwait(false);

            var updatedInDB = await DynamoDb.LoadAsync<PersonDbEntity>(person.Id).ConfigureAwait(false);
            updatedInDB.ToDomain().Should().BeEquivalentTo(person,
                                                           (e) => e.Excluding(y => y.VersionNumber)
                                                                   .Excluding(y => y.LastModified));
            updatedInDB.VersionNumber.Should().Be(person.VersionNumber + 1);
            updatedInDB.LastModified.Should().BeCloseTo(DateTime.UtcNow, 1000);

            _logger.VerifyExact(LogLevel.Debug, $"Calling IDynamoDBContext.SaveAsync for id {person.Id}", Times.Once());
        }
    }
}
