using Amazon.DynamoDBv2.DataModel;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PersonListener.Domain;
using PersonListener.Factories;
using PersonListener.Gateway;
using PersonListener.Infrastructure;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace PersonListener.Tests.Gateway
{
    [Collection("DynamoDb collection")]
    public class DynamoDbPersonGatewayTests : IDisposable
    {
        private readonly Fixture _fixture = new Fixture();
        private readonly Mock<ILogger<DynamoDbPersonGateway>> _logger;
        private readonly DynamoDbPersonGateway _classUnderTest;
        private DynamoDbFixture _dbTestFixture;
        private IDynamoDBContext DynamoDb => _dbTestFixture.DynamoDbContext;
        private readonly List<Action> _cleanup = new List<Action>();

        public DynamoDbPersonGatewayTests(DynamoDbFixture dbTestFixture)
        {
            _dbTestFixture = dbTestFixture;
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
                foreach (var action in _cleanup)
                    action();

                if (_dbTestFixture != null)
                {
                    _dbTestFixture.Dispose();
                    _dbTestFixture = null;
                }

                _disposed = true;
            }
        }

        private async Task InsertDatatoDynamoDB(Person entity)
        {
            await DynamoDb.SaveAsync(entity.ToDatabase()).ConfigureAwait(false);
            _cleanup.Add(async () => await DynamoDb.DeleteAsync<PersonDbEntity>(entity.Id).ConfigureAwait(false));
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
