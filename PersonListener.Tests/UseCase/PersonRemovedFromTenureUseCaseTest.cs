using AutoFixture;
using FluentAssertions;
using Force.DeepCloner;
using Moq;
using PersonListener.Boundary;
using PersonListener.Domain;
using PersonListener.Domain.TenureInformation;
using PersonListener.Gateway.Interfaces;
using PersonListener.Infrastructure;
using PersonListener.Infrastructure.Exceptions;
using PersonListener.UseCase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PersonListener.Tests.UseCase
{
    [Collection("LogCall collection")]

    public class PersonRemovedFromTenureUseCaseTest
    {
        private readonly Mock<IDbPersonGateway> _mockGateway;
        private readonly Mock<ITenureInfoApiGateway> _mockTenureApi;
        private readonly PersonRemovedFromTenureUseCase _sut;

        private readonly EntityEventSns _message;
        private readonly TenureResponseObject _tenure;

        private readonly Fixture _fixture;
        private static readonly Guid _correlationId = Guid.NewGuid();

        public PersonRemovedFromTenureUseCaseTest()
        {
            _fixture = new Fixture();

            _mockGateway = new Mock<IDbPersonGateway>();
            _mockTenureApi = new Mock<ITenureInfoApiGateway>();
            _sut = new PersonRemovedFromTenureUseCase(_mockGateway.Object, _mockTenureApi.Object);

            _tenure = CreateTenure();
            _message = CreateMessage(_tenure.Id);
        }

        private TenureResponseObject CreateTenure()
        {
            return _fixture.Build<TenureResponseObject>()
                           .With(x => x.HouseholdMembers, _fixture.Build<HouseholdMembers>()
                                                                  .With(x => x.DateOfBirth, DateTime.UtcNow.AddYears(-40))
                                                                  .With(x => x.PersonTenureType, "Tenant")
                                                                  .CreateMany(3).ToList())
                           .Create();
        }

        private TenureType ToTenureType(TenureType tt)
        {
            return new TenureType { Code = tt.Code, Description = tt.Description };
        }

        private TenureResponseObject CreateTenureTypeTenureForPerson(Guid tenureId, Guid personId, string personType)
        {
            TenureType tt;
            bool isResponsible;
            switch (personType)
            {
                case "HouseholderMember":
                    tt = ToTenureType(TenureTypes.Secure);
                    isResponsible = false;
                    break;
                case "Freeholder":
                    tt = ToTenureType(TenureTypes.Freehold);
                    isResponsible = true;
                    break;
                default:
                    tt = ToTenureType(TenureTypes.Secure);
                    isResponsible = true;
                    break;
            }
            var hms = _fixture.Build<HouseholdMembers>()
                              .With(x => x.DateOfBirth, DateTime.UtcNow.AddYears(-40))
                              .With(x => x.PersonTenureType, personType)
                              .With(x => x.IsResponsible, isResponsible)
                              .CreateMany(3).ToList();
            hms.Last().Id = personId;

            return _fixture.Build<TenureResponseObject>()
                           .With(x => x.Id, tenureId)
                           .With(x => x.TenureType, tt)
                           .With(x => x.HouseholdMembers, hms)
                           .Create();
        }

        private Person CreatePerson(EntityEventSns message, Guid? entityId, bool hasThisTenure = true)
        {
            if (!entityId.HasValue) return null;

            var tenures = _fixture.CreateMany<Tenure>(3).ToList();
            if (hasThisTenure)
                tenures.Last().Id = _tenure.Id;
            var personTypes = _fixture.CreateMany<PersonType>(3).ToList();
            var person = _fixture.Build<Person>()
                           .With(x => x.Id, entityId)
                           .With(x => x.Tenures, tenures)
                           .With(x => x.PersonTypes, personTypes)
                           .With(x => x.DateOfBirth, DateTime.UtcNow.AddYears(-30))
                           .Create();

            for (int i = 0; i < tenures.Count; i++)
            {
                var personTenure = tenures[i];
                var personType = personTypes[i];
                var t = CreateTenureTypeTenureForPerson(personTenure.Id, person.Id, personType.ToString());
                _mockTenureApi.Setup(x => x.GetTenureInfoByIdAsync(personTenure.Id, message.CorrelationId)).ReturnsAsync(t);
            }

            return person;
        }

        private EntityEventSns CreateMessage(Guid tenureId, string eventType = EventTypes.PersonRemovedFromTenureEvent)
        {
            return _fixture.Build<EntityEventSns>()
                           .With(x => x.EventType, eventType)
                           .With(x => x.EntityId, tenureId)
                           .With(x => x.CorrelationId, _correlationId)
                           .Create();
        }

        private Guid? SetMessageEventData(TenureResponseObject tenure, EntityEventSns message)
        {
            var oldData = tenure.HouseholdMembers;
            var newData = oldData.DeepClone();
            message.EventData = new EventData()
            {
                OldData = new Dictionary<string, object> { { "householdMembers", oldData } },
                NewData = new Dictionary<string, object> { { "householdMembers", newData } }
            };
            var removedHm = _fixture.Build<HouseholdMembers>()
                                    .With(x => x.Id, Guid.NewGuid())
                                    .With(x => x.DateOfBirth, DateTime.UtcNow.AddYears(-40))
                                    .With(x => x.PersonTenureType, "Tenant")
                                    .Create();
            var personId = removedHm.Id;
            oldData.Add(removedHm);
            return personId;
        }

        [Fact]
        public void ProcessMessageAsyncTestNullMessageThrows()
        {
            Func<Task> func = async () => await _sut.ProcessMessageAsync(null).ConfigureAwait(false);
            func.Should().ThrowAsync<ArgumentNullException>();
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
        public void ProcessMessageAsyncTestNoChangedHouseholdMembersThrows()
        {
            SetMessageEventData(_tenure, _message);

            _mockTenureApi.Setup(x => x.GetTenureInfoByIdAsync(_message.EntityId, _message.CorrelationId))
                                       .ReturnsAsync(_tenure);
            Func<Task> func = async () => await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);
            func.Should().ThrowAsync<HouseholdMembersNotChangedException>();
        }

        [Fact]
        public void ProcessMessageAsyncTestPersonIdNotFoundThrows()
        {
            var personId = SetMessageEventData(_tenure, _message);
            var person = CreatePerson(_message, personId);
            _mockGateway.Setup(x => x.GetPersonByIdAsync(person.Id)).ReturnsAsync(person);

            _mockGateway.Setup(x => x.GetPersonByIdAsync(person.Id)).ReturnsAsync((Person) null);
            Func<Task> func = async () => await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);
            func.Should().ThrowAsync<EntityNotFoundException<Person>>();
        }

        [Fact]
        public void ProcessMessageAsyncTestIndexTenureExceptionThrows()
        {
            var personId = SetMessageEventData(_tenure, _message);
            var person = CreatePerson(_message, personId);
            _mockGateway.Setup(x => x.GetPersonByIdAsync(person.Id))
                                       .ReturnsAsync(person);

            _mockTenureApi.Setup(x => x.GetTenureInfoByIdAsync(person.Id, _correlationId)).ReturnsAsync(_tenure);


            var exMsg = "This is the last error";
            _mockGateway.Setup(x => x.SavePersonAsync(It.IsAny<Person>()))
                        .ThrowsAsync(new Exception(exMsg));

            Func<Task> func = async () => await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);
            func.Should().ThrowAsync<Exception>().WithMessage(exMsg);

            _mockGateway.Verify(x => x.GetPersonByIdAsync(person.Id), Times.Once);
        }

        [Fact]
        public void ProcessMessageAsyncTestIndexPersonExceptionThrows()
        {
            var personId = SetMessageEventData(_tenure, _message);
            var person = CreatePerson(_message, personId);
            _mockGateway.Setup(x => x.GetPersonByIdAsync(person.Id))
                                       .ReturnsAsync(person);
            var exMsg = "This is the last error";
            _mockGateway.Setup(x => x.SavePersonAsync(It.IsAny<Person>()))
                        .ThrowsAsync(new Exception(exMsg));


            Func<Task> func = async () => await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);
            func.Should().ThrowAsync<Exception>().WithMessage(exMsg);

            _mockGateway.Verify(x => x.GetPersonByIdAsync(person.Id), Times.Once);
        }

        [Fact]
        public async Task ProcessMessageAsyncTestIndexBothSuccess()
        {
            var personId = SetMessageEventData(_tenure, _message);

            var person = CreatePerson(_message, personId);
            var startingPerson = person.DeepClone();
            _mockGateway.Setup(x => x.GetPersonByIdAsync(person.Id))
                                       .ReturnsAsync(person);

            await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);

            _mockGateway.Verify(x => x.GetPersonByIdAsync(person.Id), Times.Once);
            _mockGateway.Verify(x => x.SavePersonAsync(person), Times.Once);
        }

    }
}
