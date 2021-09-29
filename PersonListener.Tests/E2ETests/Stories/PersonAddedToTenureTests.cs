using PersonListener.Tests.E2ETests.Fixtures;
using PersonListener.Tests.E2ETests.Steps;
using System;
using TestStack.BDDfy;
using Xunit;

namespace PersonListener.Tests.E2ETests.Stories
{
    [Story(
        AsA = "SQS Entity Listener",
        IWant = "a function to process the PersonAddedToTenure message",
        SoThat = "The correct details are set on the person")]
    [Collection("DynamoDb collection")]
    public class PersonAddedToTenureTests : IDisposable
    {
        private readonly DynamoDbFixture _dbFixture;
        private readonly PersonFixture _personFixture;
        private readonly TenureApiFixture _tenureApiFixture;

        private readonly PersonAddedToTenureUseCaseSteps _steps;

        public PersonAddedToTenureTests(DynamoDbFixture dbFixture)
        {
            _dbFixture = dbFixture;

            _personFixture = new PersonFixture(_dbFixture.DynamoDbContext);
            _tenureApiFixture = new TenureApiFixture();

            _steps = new PersonAddedToTenureUseCaseSteps();
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
        public void ListenerUpdatesThePerson()
        {
            var tenureId = Guid.NewGuid();
            this.Given(g => _tenureApiFixture.GivenTheTenureExists(tenureId))
                .And(h => _steps.GivenAMessageWithPersonAdded(TenureApiFixture.TenureResponse))
                .And(h => _personFixture.GivenAPersonAlreadyExists(_steps.NewPersonId))
                .When(w => _steps.WhenTheFunctionIsTriggered(_steps.TheMessage))
                .Then(t => _steps.ThenTheCorrleationIdWasUsedInTheApiCall(_tenureApiFixture.ReceivedCorrelationIds))
                .Then(t => _steps.ThenThePersonIsUpdated(_personFixture.DbEntity, TenureApiFixture.TenureResponse,
                                                         _dbFixture.DynamoDbContext))
                .BDDfy();
        }

        [Fact]
        public void TenureNotFound()
        {
            var tenureId = Guid.NewGuid();
            this.Given(g => _tenureApiFixture.GivenTheTenureDoesNotExist(tenureId))
                .When(w => _steps.WhenTheFunctionIsTriggered(tenureId))
                .Then(t => _steps.ThenATenureNotFoundExceptionIsThrown(tenureId))
                .Then(t => _steps.ThenTheCorrleationIdWasUsedInTheApiCall(_tenureApiFixture.ReceivedCorrelationIds))
                .BDDfy();
        }

        [Fact]
        public void NoPersonAdded()
        {
            var tenureId = Guid.NewGuid();
            this.Given(g => _tenureApiFixture.GivenTheTenureExists(tenureId))
                .And(h => _steps.GivenAMessageWithNoPersonAdded(TenureApiFixture.TenureResponse))
                .When(w => _steps.WhenTheFunctionIsTriggered(_steps.TheMessage))
                .Then(t => _steps.ThenTheCorrleationIdWasUsedInTheApiCall(_tenureApiFixture.ReceivedCorrelationIds))
                .Then(t => _steps.ThenAHouseholdMembersNotChangedExceptionIsThrown(tenureId))
                .BDDfy();
        }

        [Fact]
        public void PersonNotFound()
        {
            var tenureId = Guid.NewGuid();
            this.Given(g => _tenureApiFixture.GivenTheTenureExists(tenureId))
                .And(h => _steps.GivenAMessageWithPersonAdded(TenureApiFixture.TenureResponse))
                .And(h => _personFixture.GivenAPersonDoesNotExist(_steps.NewPersonId))
                .When(w => _steps.WhenTheFunctionIsTriggered(_steps.TheMessage))
                .Then(t => _steps.ThenTheCorrleationIdWasUsedInTheApiCall(_tenureApiFixture.ReceivedCorrelationIds))
                .Then(t => _steps.ThenAPersonNotFoundExceptionIsThrown(_steps.NewPersonId))
                .BDDfy();
        }
    }
}
