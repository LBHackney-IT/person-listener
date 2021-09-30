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
    [Collection("DynamoDb collection")]
    public class TenureUpdatedTests : IDisposable
    {
        private readonly DynamoDbFixture _dbFixture;
        private readonly PersonFixture _personFixture;
        private readonly TenureApiFixture _tenureApiFixture;

        private readonly TenureUpdatedUseCaseSteps _steps;

        public TenureUpdatedTests(DynamoDbFixture dbFixture)
        {
            _dbFixture = dbFixture;

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
                .And(h => _personFixture.GivenThePersonsAlreadyExist(TenureApiFixture.TenureResponse))
                .When(w => _steps.WhenTheFunctionIsTriggered(tenureId))
                .Then(t => _steps.ThenTheCorrleationIdWasUsedInTheApiCall(_tenureApiFixture.ReceivedCorrelationIds))
                .Then(t => _steps.ThenThePersonsAreUpdated(_personFixture.PersonsDbEntity, TenureApiFixture.TenureResponse,
                                                         _dbFixture.DynamoDbContext))
                .BDDfy();
        }

        [Fact]
        public void TenureNotFound()
        {
            var tenureId = Guid.NewGuid();
            this.Given(g => _tenureApiFixture.GivenTheTenureDoesNotExist(tenureId))
                .When(w => _steps.WhenTheFunctionIsTriggered(tenureId))
                .Then(t => _steps.ThenTheCorrleationIdWasUsedInTheApiCall(_tenureApiFixture.ReceivedCorrelationIds))
                .Then(t => _steps.ThenATenureNotFoundExceptionIsThrown(tenureId))
                .BDDfy();
        }

        [Fact]
        public void TenureHasdNoHouseholdMembers()
        {
            var tenureId = Guid.NewGuid();
            this.Given(g => _tenureApiFixture.GivenTheTenureExists(tenureId))
                .And(g => _tenureApiFixture.GivenTheTenureHasNoHouseholdMembers(TenureApiFixture.TenureResponse))
                .When(w => _steps.WhenTheFunctionIsTriggered(tenureId))
                .Then(t => _steps.ThenTheCorrleationIdWasUsedInTheApiCall(_tenureApiFixture.ReceivedCorrelationIds))
                .Then(t => _steps.TheNoExceptionIsThrown())
                .BDDfy();
        }

        [Fact]
        public void PersonNotFound()
        {
            var tenureId = Guid.NewGuid();
            this.Given(g => _tenureApiFixture.GivenTheTenureExists(tenureId))
                .And(h => _personFixture.GivenAPersonDoesNotExist(
                    TenureApiFixture.TenureResponse.HouseholdMembers.First().Id))
                .When(w => _steps.WhenTheFunctionIsTriggered(tenureId))
                .Then(t => _steps.ThenTheCorrleationIdWasUsedInTheApiCall(_tenureApiFixture.ReceivedCorrelationIds))
                .Then(t => _steps.ThenAnAggregatedPersonNotFoundExceptionIsThrown(
                    TenureApiFixture.TenureResponse.HouseholdMembers.Select(x => x.Id)))
                .BDDfy();
        }
    }
}
