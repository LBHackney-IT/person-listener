using Hackney.Core.Testing.DynamoDb;
using PersonListener.Tests.E2ETests.Fixtures;
using PersonListener.Tests.E2ETests.Steps;
using System;
using TestStack.BDDfy;
using Xunit;

namespace PersonListener.Tests.E2ETests.Stories
{
    [Story(
        AsA = "SQS Tenure Listener",
        IWant = "a function to process the RemovePersonFromTenure message",
        SoThat = "The tenure and person details are set correctly")]
    [Collection("AppTest collection")]
    public class RemovePersonFromTenureTests : IDisposable
    {
        private readonly IDynamoDbFixture _dbFixture;
        private readonly PersonFixture _personFixture;
        private readonly TenureApiFixture _tenureApiFixture;

        private readonly PersonRemovedFromTenureStep _steps;

        public RemovePersonFromTenureTests(MockApplicationFactory appFactory)
        {
            _dbFixture = appFactory.DynamoDbFixture;
            _personFixture = new PersonFixture(_dbFixture.DynamoDbContext);
            _tenureApiFixture = new TenureApiFixture();
            _steps = new PersonRemovedFromTenureStep();
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
        public void PersonNotFound()
        {
            var tenureId = Guid.NewGuid();
            var removedPersonId = Guid.NewGuid();
            this.Given(g => _tenureApiFixture.GivenAPersonWasRemoved(removedPersonId))
                .And(g => _personFixture.GivenAPersonDoesNotExist(removedPersonId))
                .When(w => _steps.WhenTheFunctionIsTriggered(tenureId, _tenureApiFixture.MessageEventData, EventTypes.PersonRemovedFromTenureEvent))
                .Then(t => _steps.ThenAPersonNotFoundExceptionIsThrown(removedPersonId))
                .BDDfy();
        }

        [Fact]
        public void TenureRemovedFromPerson()
        {
            var tenureId = Guid.NewGuid();
            var removedPersonId = Guid.NewGuid();
            _ = this.Given(g => _tenureApiFixture.GivenAPersonWasRemoved(removedPersonId))
                .And(g => _personFixture.GivenThePersonExistsWithTenure(removedPersonId, tenureId))
                .And(g => _tenureApiFixture.GivenThePersonTenuresExist(_personFixture.DbEntity))
                .When(w => _steps.WhenTheFunctionIsTriggered(tenureId, _tenureApiFixture.MessageEventData, EventTypes.PersonRemovedFromTenureEvent))
                .Then(t => _steps.ThenThePersonHasTheTenureRemoved(removedPersonId, tenureId, _dbFixture.DynamoDbContext))
                .Then(t => _steps.ThenTheCorrelationIdWasUsedInTheApiCall(_tenureApiFixture.ReceivedCorrelationIds))
                .BDDfy();
        }
    }
}
