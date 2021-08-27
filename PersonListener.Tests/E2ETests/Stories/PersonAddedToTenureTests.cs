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

        private readonly PersonAddedToTenureUseCaseSteps _steps;

        public PersonAddedToTenureTests(DynamoDbFixture dbFixture)
        {
            _dbFixture = dbFixture;

            _personFixture = new PersonFixture(_dbFixture.DynamoDbContext);

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

                _disposed = true;
            }
        }

        [Fact]
        public void ListenerUpdatesThePerson()
        {
            var id = Guid.NewGuid();
            this.Given(g => _personFixture.GivenAPersonAlreadyExists(id))
                .When(w => _steps.WhenTheFunctionIsTriggered(id))
                .Then(t => _steps.ThenTheEntityIsUpdated(_personFixture.DbEntity, _dbFixture.DynamoDbContext))
                .BDDfy();
        }

        [Fact]
        public void PersonNotFound()
        {
            var id = Guid.NewGuid();
            this.Given(g => _personFixture.GivenAPersonDoesNotExist(id))
                .When(w => _steps.WhenTheFunctionIsTriggered(id))
                .Then(t => _steps.ThenAnEntityNotFoundExceptionIsThrown(id))
                .BDDfy();
        }
    }
}
