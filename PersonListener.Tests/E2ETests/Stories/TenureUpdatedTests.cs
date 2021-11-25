using Hackney.Core.Testing.DynamoDb;
using PersonListener.Tests.E2ETests.Fixtures;
using PersonListener.Tests.E2ETests.Steps;
using System;
using System.Linq;
using TestStack.BDDfy;
using Xunit;

namespace PersonListener.Tests.E2ETests.Stories
{
    [Story(
        AsA = "SQS Entity Listener",
        IWant = "a function to process the TenureUpdated message",
        SoThat = "The correct details are set on the appropriate persons")]
    [Collection("AppTest collection")]
    public class TenureUpdatedTests : IDisposable
    {
        private readonly IDynamoDbFixture _dbFixture;
        private readonly PersonFixture _personFixture;
        private readonly TenureApiFixture _tenureApiFixture;

        private readonly TenureUpdatedUseCaseSteps _steps;

        public TenureUpdatedTests(MockApplicationFactory appFactory)
        {
            _dbFixture = appFactory.DynamoDbFixture;

            _personFixture = new PersonFixture(_dbFixture.DynamoDbContext);
            _tenureApiFixture = new TenureApiFixture();

            _steps = new TenureUpdatedUseCaseSteps();
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
                _personFixture.Dispose();
                _tenureApiFixture.Dispose();

                _disposed = true;
            }
        }

        [Fact]
        public void ListenerUpdatesThePersons()
        {
            var tenureId = Guid.NewGuid();
            this.Given(g => _tenureApiFixture.GivenTheTenureExists(tenureId))
                .And(h => _personFixture.GivenThePersonsAlreadyExist(_tenureApiFixture.ResponseObject))
                .When(w => _steps.WhenTheFunctionIsTriggered(tenureId))
                .Then(t => _steps.ThenTheCorrelationIdWasUsedInTheApiCall(_tenureApiFixture.ReceivedCorrelationIds))
                .Then(t => _steps.ThenThePersonsAreUpdated(_personFixture.PersonsDbEntity, _tenureApiFixture.ResponseObject,
                                                         _dbFixture.DynamoDbContext))
                .BDDfy();
        }

        [Fact]
        public void TenureNotFound()
        {
            var tenureId = Guid.NewGuid();
            this.Given(g => _tenureApiFixture.GivenTheTenureDoesNotExist(tenureId))
                .When(w => _steps.WhenTheFunctionIsTriggered(tenureId))
                .Then(t => _steps.ThenTheCorrelationIdWasUsedInTheApiCall(_tenureApiFixture.ReceivedCorrelationIds))
                .Then(t => _steps.ThenATenureNotFoundExceptionIsThrown(tenureId))
                .BDDfy();
        }

        [Fact]
        public void TenureHasNoHouseholdMembers()
        {
            var tenureId = Guid.NewGuid();
            this.Given(g => _tenureApiFixture.GivenTheTenureExists(tenureId))
                .And(g => _tenureApiFixture.GivenTheTenureHasNoHouseholdMembers(_tenureApiFixture.ResponseObject))
                .When(w => _steps.WhenTheFunctionIsTriggered(tenureId))
                .Then(t => _steps.ThenTheCorrelationIdWasUsedInTheApiCall(_tenureApiFixture.ReceivedCorrelationIds))
                .Then(t => _steps.TheNoExceptionIsThrown())
                .BDDfy();
        }

        [Fact]
        public void PersonNotFound()
        {
            var tenureId = Guid.NewGuid();
            this.Given(g => _tenureApiFixture.GivenTheTenureExists(tenureId))
                .And(h => _personFixture.GivenAPersonDoesNotExist(
                    _tenureApiFixture.ResponseObject.HouseholdMembers.First().Id))
                .When(w => _steps.WhenTheFunctionIsTriggered(tenureId))
                .Then(t => _steps.ThenTheCorrelationIdWasUsedInTheApiCall(_tenureApiFixture.ReceivedCorrelationIds))
                .Then(t => _steps.ThenAnAggregatedPersonNotFoundExceptionIsThrown(
                    _tenureApiFixture.ResponseObject.HouseholdMembers.Select(x => x.Id)))
                .BDDfy();
        }
    }
}
