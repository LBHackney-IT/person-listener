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
        IWant = "a function to process the AccountCreated message",
        SoThat = "The correct details are set on the appropriate persons")]
    [Collection("AppTest collection")]
    public class AccountCreatedUpdatesPersonTenureTests : IDisposable
    {
        private readonly IDynamoDbFixture _dbFixture;
        private readonly PersonFixture _personFixture;
        private readonly TenureApiFixture _tenureApiFixture;
        private readonly AccountApiFixture _accountApiFixture;

        private readonly AccountCreatedUseCaseSteps _steps;

        public AccountCreatedUpdatesPersonTenureTests(MockApplicationFactory appFactory)
        {
            _dbFixture = appFactory.DynamoDbFixture;

            _personFixture = new PersonFixture(_dbFixture.DynamoDbContext);
            _tenureApiFixture = new TenureApiFixture();
            _accountApiFixture = new AccountApiFixture();

            _steps = new AccountCreatedUseCaseSteps();
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
                _accountApiFixture.Dispose();

                _disposed = true;
            }
        }

        [Fact]
        public void ListenerUpdatesThePersons()
        {
            var accountId = Guid.NewGuid();
            this.Given(g => _accountApiFixture.GivenTheAccountExists(accountId))
                .And(h => _tenureApiFixture.GivenTheTenureExists(_accountApiFixture.ResponseObject.TargetId))
                .And(i => _personFixture.GivenThePersonsAlreadyExist(_tenureApiFixture.ResponseObject))
                .When(w => _steps.WhenTheFunctionIsTriggered(accountId))
                .Then(t => _steps.ThenTheCorrelationIdWasUsedInTheApiCall(_accountApiFixture.ReceivedCorrelationIds))
                .Then(t => _steps.ThenTheCorrelationIdWasUsedInTheApiCall(_tenureApiFixture.ReceivedCorrelationIds))
                .Then(t => _steps.ThenThePersonsAreUpdated(_personFixture.PersonsDbEntity, _tenureApiFixture.ResponseObject,
                                                           _accountApiFixture.ResponseObject, _dbFixture.DynamoDbContext))
                .BDDfy();
        }

        [Fact]
        public void AccountNotFound()
        {
            var accountId = Guid.NewGuid();
            this.Given(g => _accountApiFixture.GivenTheAccountDoesNotExist(accountId))
                .When(w => _steps.WhenTheFunctionIsTriggered(accountId))
                .Then(t => _steps.ThenAnAccountNotFoundExceptionIsThrown(accountId))
                .Then(t => _steps.ThenTheCorrelationIdWasUsedInTheApiCall(_accountApiFixture.ReceivedCorrelationIds))
                .BDDfy();
        }

        [Fact]
        public void TenureNotFound()
        {
            var accountId = Guid.NewGuid();
            this.Given(g => _accountApiFixture.GivenTheAccountExists(accountId))
                .And(h => _tenureApiFixture.GivenTheTenureDoesNotExist(_accountApiFixture.ResponseObject.TargetId))
                .When(w => _steps.WhenTheFunctionIsTriggered(accountId))
                .Then(t => _steps.ThenTheCorrelationIdWasUsedInTheApiCall(_accountApiFixture.ReceivedCorrelationIds))
                .Then(t => _steps.ThenTheCorrelationIdWasUsedInTheApiCall(_tenureApiFixture.ReceivedCorrelationIds))
                .Then(t => _steps.ThenATenureNotFoundExceptionIsThrown(_accountApiFixture.ResponseObject.TargetId))
                .BDDfy();
        }

        [Fact]
        public void TenureHasNoHouseholdMembers()
        {
            var accountId = Guid.NewGuid();
            this.Given(g => _accountApiFixture.GivenTheAccountExists(accountId))
                .And(h => _tenureApiFixture.GivenTheTenureExists(_accountApiFixture.ResponseObject.TargetId))
                .And(g => _tenureApiFixture.GivenTheTenureHasNoHouseholdMembers(_tenureApiFixture.ResponseObject))
                .When(w => _steps.WhenTheFunctionIsTriggered(accountId))
                .Then(t => _steps.ThenTheCorrelationIdWasUsedInTheApiCall(_accountApiFixture.ReceivedCorrelationIds))
                .Then(t => _steps.ThenTheCorrelationIdWasUsedInTheApiCall(_tenureApiFixture.ReceivedCorrelationIds))
                .Then(t => _steps.TheNoExceptionIsThrown())
                .BDDfy();
        }

        [Fact]
        public void PersonNotFound()
        {
            var accountId = Guid.NewGuid();
            this.Given(g => _accountApiFixture.GivenTheAccountExists(accountId))
                .And(h => _tenureApiFixture.GivenTheTenureExists(_accountApiFixture.ResponseObject.TargetId))
                .And(h => _personFixture.GivenAPersonDoesNotExist(
                    _tenureApiFixture.ResponseObject.HouseholdMembers.First().Id))
                .When(w => _steps.WhenTheFunctionIsTriggered(accountId))
                .Then(t => _steps.ThenTheCorrelationIdWasUsedInTheApiCall(_accountApiFixture.ReceivedCorrelationIds))
                .Then(t => _steps.ThenTheCorrelationIdWasUsedInTheApiCall(_tenureApiFixture.ReceivedCorrelationIds))
                .Then(t => _steps.ThenAnAggregatedPersonNotFoundExceptionIsThrown(
                    _tenureApiFixture.ResponseObject.HouseholdMembers.Select(x => x.Id)))
                .BDDfy();
        }
    }
}
