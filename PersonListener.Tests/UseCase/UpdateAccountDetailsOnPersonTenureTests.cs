using AutoFixture;
using FluentAssertions;
using Hackney.Shared.Person;
using Hackney.Shared.Person.Domain;
using Hackney.Shared.Tenure.Boundary.Response;
using Hackney.Shared.Tenure.Domain;
using Moq;
using PersonListener.Boundary;
using PersonListener.Domain.Account;
using PersonListener.Gateway.Interfaces;
using PersonListener.Infrastructure.Exceptions;
using PersonListener.UseCase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace PersonListener.Tests.UseCase
{
    [Collection("LogCall collection")]
    public class UpdateAccountDetailsOnPersonTenureTests
    {
        private readonly Mock<IDbPersonGateway> _mockGateway;
        private readonly Mock<ITenureInfoApiGateway> _mockTenureApi;
        private readonly Mock<IAccountApi> _mockAccountApi;
        private readonly UpdateAccountDetailsOnPersonTenure _sut;

        private readonly EntityEventSns _message;
        private readonly AccountResponseObject _account;
        private readonly TenureResponseObject _tenure;

        private readonly Fixture _fixture;
        private static readonly Guid _correlationId = Guid.NewGuid();
        private const string DateTimeFormat = "yyyy-MM-ddTHH\\:mm\\:ss.fffffffZ";

        public UpdateAccountDetailsOnPersonTenureTests()
        {
            _fixture = new Fixture();

            _mockGateway = new Mock<IDbPersonGateway>();
            _mockTenureApi = new Mock<ITenureInfoApiGateway>();
            _mockAccountApi = new Mock<IAccountApi>();
            _sut = new UpdateAccountDetailsOnPersonTenure(_mockGateway.Object, _mockAccountApi.Object, _mockTenureApi.Object);

            _account = CreateAccount();
            _tenure = CreateTenure(_account.Tenure.TenancyId);
            _message = CreateMessage(_account.Id);

            _mockAccountApi.Setup(x => x.GetAccountByIdAsync(_message.EntityId, _message.CorrelationId))
                           .ReturnsAsync(_account);

            _mockTenureApi.Setup(x => x.GetTenureInfoByIdAsync(_account.Tenure.TenancyId, _message.CorrelationId))
                                       .ReturnsAsync(_tenure);
        }

        private AccountResponseObject CreateAccount()
        {
            return _fixture.Build<AccountResponseObject>()
                           .With(x => x.StartDate, DateTime.UtcNow.AddMonths(-1).ToString(DateTimeFormat))
                           .Create();
        }

        private TenureResponseObject CreateTenure(Guid entityId)
        {
            return _fixture.Build<TenureResponseObject>()
                           .With(x => x.Id, entityId)
                           .With(x => x.HouseholdMembers, _fixture.Build<HouseholdMembers>()
                                                                  .With(x => x.PersonTenureType, PersonTenureType.Tenant)
                                                                  .CreateMany(3)
                                                                  .ToList())
                           .Create();
        }

        private Person CreatePerson(Guid? entityId)
        {
            if (!entityId.HasValue) return null;

            return _fixture.Build<Person>()
                           .With(x => x.Id, entityId)
                           .With(x => x.PersonTypes, new[] { PersonType.Tenant })
                           .Create();
        }

        private EntityEventSns CreateMessage(Guid tenureId, string eventType = EventTypes.AccountCreatedEvent)
        {
            return _fixture.Build<EntityEventSns>()
                           .With(x => x.EventType, eventType)
                           .With(x => x.EntityId, tenureId)
                           .With(x => x.CorrelationId, _correlationId)
                           .Create();
        }

        private List<Person> SetupPersonTenures()
        {
            var persons = new List<Person>();
            foreach (var hm in _tenure.HouseholdMembers)
            {
                var person = CreatePerson(hm.Id);
                person.Tenures.First().Id = _tenure.Id;
                _mockGateway.Setup(x => x.GetPersonByIdAsync(person.Id)).ReturnsAsync(person);
                persons.Add(person);
            }
            return persons;
        }

        [Fact]
        public void ProcessMessageAsyncTestNullMessageThrows()
        {
            Func<Task> func = async () => await _sut.ProcessMessageAsync(null).ConfigureAwait(false);
            func.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public void ProcessMessageAsyncTestGetAccountExceptionThrown()
        {
            var exMsg = "This is an error";
            _mockAccountApi.Setup(x => x.GetAccountByIdAsync(_message.EntityId, _message.CorrelationId))
                           .ThrowsAsync(new Exception(exMsg));

            Func<Task> func = async () => await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);
            func.Should().ThrowAsync<Exception>().WithMessage(exMsg);
        }

        [Fact]
        public void ProcessMessageAsyncTestGetAccountReturnsNullThrows()
        {
            _mockAccountApi.Setup(x => x.GetAccountByIdAsync(_message.EntityId, _message.CorrelationId))
                           .ReturnsAsync((AccountResponseObject) null);

            Func<Task> func = async () => await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);
            func.Should().ThrowAsync<EntityNotFoundException<AccountResponseObject>>();
        }

        [Fact]
        public void ProcessMessageAsyncTestGetTenureExceptionThrown()
        {
            var exMsg = "This is an error";
            _mockTenureApi.Setup(x => x.GetTenureInfoByIdAsync(_message.EntityId, _message.CorrelationId))
                          .ThrowsAsync(new Exception(exMsg));

            Func<Task> func = async () => await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);
            func.Should().ThrowAsync<Exception>().WithMessage(exMsg);
        }

        [Fact]
        public void ProcessMessageAsyncTestGetTenureReturnsNullThrows()
        {
            _mockTenureApi.Setup(x => x.GetTenureInfoByIdAsync(_message.EntityId, _message.CorrelationId))
                          .ReturnsAsync((TenureResponseObject) null);

            Func<Task> func = async () => await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);
            func.Should().ThrowAsync<EntityNotFoundException<TenureResponseObject>>();
        }

        [Fact]
        public async Task ProcessMessageAsyncTestNullHouseholdMembersDoesNothing()
        {
            _tenure.HouseholdMembers = null;
            await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);

            _mockGateway.Verify(x => x.GetPersonByIdAsync(It.IsAny<Guid>()), Times.Never());
            _mockGateway.Verify(x => x.SavePersonAsync(It.IsAny<Person>()), Times.Never());
        }

        [Fact]
        public async Task ProcessMessageAsyncTestEmptyHouseholdMembersDoesNothing()
        {
            _tenure.HouseholdMembers.Clear();
            await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);

            _mockGateway.Verify(x => x.GetPersonByIdAsync(It.IsAny<Guid>()), Times.Never());
            _mockGateway.Verify(x => x.SavePersonAsync(It.IsAny<Person>()), Times.Never());
        }

        [Fact]
        public void ProcessMessageAsyncTestMissingPersonThrows()
        {
            Func<Task> func = async () => await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);
            func.Should().Throw<EntityNotFoundException<Person>>();

            _mockGateway.Verify(x => x.GetPersonByIdAsync(It.IsAny<Guid>()), Times.Exactly(3));
            _mockGateway.Verify(x => x.SavePersonAsync(It.IsAny<Person>()), Times.Never());
        }

        [Fact]
        public void ProcessMessageAsyncTestMissingPersonTenureThrows()
        {
            var person = CreatePerson(_tenure.HouseholdMembers.First().Id);
            _mockGateway.Setup(x => x.GetPersonByIdAsync(person.Id)).ReturnsAsync(person);

            Func<Task> func = async () => await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);
            func.Should().Throw<PersonMissingTenureException>();

            _mockGateway.Verify(x => x.GetPersonByIdAsync(person.Id), Times.Once());
            _mockGateway.Verify(x => x.SavePersonAsync(It.IsAny<Person>()), Times.Never());
        }

        [Fact]
        public void ProcessMessageAsyncTestSavePersonExceptionThrown()
        {
            SetupPersonTenures();

            var errorPersonId = _tenure.HouseholdMembers.Last().Id;
            var exMsg = "Some error";
            _mockGateway.Setup(x => x.SavePersonAsync(It.Is<Person>(y => y.Id == errorPersonId)))
                        .ThrowsAsync(new Exception(exMsg));

            Func<Task> func = async () => await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);
            func.Should().ThrowAsync<Exception>().WithMessage(exMsg);

            _mockGateway.Verify(x => x.GetPersonByIdAsync(It.IsAny<Guid>()), Times.Exactly(3));
            _mockGateway.Verify(x => x.SavePersonAsync(It.IsAny<Person>()), Times.Exactly(3));
        }

        [Fact]
        public async Task ProcessMessageAsyncTestAllPersonsUpdated()
        {
            var persons = SetupPersonTenures();

            await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);

            foreach (var p in persons)
            {
                _mockGateway.Verify(x => x.GetPersonByIdAsync(p.Id), Times.Once());
                _mockGateway.Verify(x => x.SavePersonAsync(It.Is<Person>(
                                                y => y.Id == p.Id && VerifyPersonTenureUpdated(y, _tenure, _account))),
                                    Times.Once());
            }
        }

        private bool VerifyPersonTenureUpdated(Person p, TenureResponseObject tenure, AccountResponseObject account)
        {
            var pt = p.Tenures.First(x => x.Id == tenure.Id);
            pt.PaymentReference.Should().Be(account.PaymentReference);

            return true;
        }
    }
}
