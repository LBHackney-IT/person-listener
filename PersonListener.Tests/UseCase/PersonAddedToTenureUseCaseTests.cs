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
using System.Threading.Tasks;
using Xunit;

namespace PersonListener.Tests.UseCase
{
    [Collection("LogCall collection")]
    public class PersonAddedToTenureUseCaseTests
    {
        private readonly Mock<IDbPersonGateway> _mockGateway;
        private readonly Mock<ITenureInfoApiGateway> _mockTenureApi;
        private readonly PersonAddedToTenureUseCase _sut;

        private readonly EntityEventSns _message;
        private readonly TenureResponseObject _tenure;

        private readonly Fixture _fixture;
        private static readonly Guid _correlationId = Guid.NewGuid();

        public PersonAddedToTenureUseCaseTests()
        {
            _fixture = new Fixture();

            _mockGateway = new Mock<IDbPersonGateway>();
            _mockTenureApi = new Mock<ITenureInfoApiGateway>();
            _sut = new PersonAddedToTenureUseCase(_mockGateway.Object, _mockTenureApi.Object);

            _tenure = CreateTenure();
            _message = CreateMessage(_tenure.Id);
        }

        private TenureResponseObject CreateTenure()
        {
            return _fixture.Build<TenureResponseObject>()
                           .With(x => x.HouseholdMembers, _fixture.Build<HouseholdMembers>()
                                                                  .With(x => x.PersonTenureType, "Tenant")
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

        private EntityEventSns CreateMessage(Guid tenureId, string eventType = EventTypes.PersonAddedToTenureEvent)
        {
            return _fixture.Build<EntityEventSns>()
                           .With(x => x.EventType, eventType)
                           .With(x => x.EntityId, tenureId)
                           .With(x => x.CorrelationId, _correlationId)
                           .Create();
        }

        private Guid? SetMessageEventData(TenureResponseObject tenure, EntityEventSns message, bool hasChanges, HouseholdMembers added = null)
        {
            var oldData = tenure.HouseholdMembers;
            var newData = oldData.DeepClone();
            message.EventData = new EventData()
            {
                OldData = new Dictionary<string, object> { { "householdMembers", oldData } },
                NewData = new Dictionary<string, object> { { "householdMembers", newData } }
            };

            Guid? personId = null;
            if (hasChanges)
            {
                if (added is null)
                {
                    var changed = newData.First();
                    changed.FullName = "Updated name";
                    personId = changed.Id;
                }
                else
                {
                    newData.Add(added);
                    personId = added.Id;
                }
            }
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
            SetMessageEventData(_tenure, _message, false);

            _mockTenureApi.Setup(x => x.GetTenureInfoByIdAsync(_message.EntityId, _message.CorrelationId))
                                       .ReturnsAsync(_tenure);
            Func<Task> func = async () => await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);
            func.Should().ThrowAsync<HouseholdMembersNotChangedException>();
        }

        [Fact]
        public void ProcessMessageAsyncTestPersonIdNotFoundThrows()
        {
            var personId = SetMessageEventData(_tenure, _message, true);
            var person = CreatePerson(personId);
            _mockGateway.Setup(x => x.GetPersonByIdAsync(person.Id)).ReturnsAsync(person);

            _mockTenureApi.Setup(x => x.GetTenureInfoByIdAsync(_message.EntityId, _message.CorrelationId))
                                       .ReturnsAsync(_tenure);
            _mockGateway.Setup(x => x.GetPersonByIdAsync(person.Id)).ReturnsAsync((Person) null);
            Func<Task> func = async () => await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);
            func.Should().ThrowAsync<EntityNotFoundException<Person>>();
        }

        [Fact]
        public void ProcessMessageAsyncTestSaveEntityThrows()
        {
            var personId = SetMessageEventData(_tenure, _message, true);
            var person = CreatePerson(personId);
            _mockGateway.Setup(x => x.GetPersonByIdAsync(person.Id)).ReturnsAsync(person);

            _mockTenureApi.Setup(x => x.GetTenureInfoByIdAsync(_message.EntityId, _message.CorrelationId))
                                       .ReturnsAsync(_tenure);
            var exMsg = "This is the last error";
            _mockGateway.Setup(x => x.SavePersonAsync(It.IsAny<Person>()))
                        .ThrowsAsync(new Exception(exMsg));

            Func<Task> func = async () => await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);
            func.Should().ThrowAsync<Exception>().WithMessage(exMsg);

            _mockGateway.Verify(x => x.GetPersonByIdAsync(person.Id), Times.Once);
            _mockGateway.Verify(x => x.SavePersonAsync(person), Times.Once);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task ProcessMessageAsyncTestSaveEntitySucceedsAddsNewTenure(bool added, bool addsPersonType)
        {
            HouseholdMembers newHm = null;
            if (added)
            {
                newHm = _fixture.Build<HouseholdMembers>()
                                .With(x => x.PersonTenureType, addsPersonType ? "Occupant" : "Tenant")
                                .Create();
            }

            var personId = SetMessageEventData(_tenure, _message, true, newHm);
            var changedHm = added ? newHm
                : (_message.EventData.NewData["householdMembers"] as List<HouseholdMembers>).First();
            var person = CreatePerson(personId);
            _mockGateway.Setup(x => x.GetPersonByIdAsync(person.Id)).ReturnsAsync(person);

            _mockTenureApi.Setup(x => x.GetTenureInfoByIdAsync(_message.EntityId, _message.CorrelationId))
                                       .ReturnsAsync(_tenure);

            await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);

            _mockGateway.Verify(x => x.GetPersonByIdAsync(person.Id), Times.Once);
            _mockGateway.Verify(x => x.SavePersonAsync(It.Is<Person>(y => VerifyUpdatedPerson(y, _tenure, changedHm))),
                                Times.Once);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task ProcessMessageAsyncTestSaveEntitySucceedsExistingTenure(bool added, bool addsPersonType)
        {
            HouseholdMembers newHm = null;
            if (added)
            {
                newHm = _fixture.Build<HouseholdMembers>()
                                .With(x => x.PersonTenureType, addsPersonType ? "Occupant" : "Tenant")
                                .Create();
            }

            var personId = SetMessageEventData(_tenure, _message, true, newHm);
            var changedHm = added ? newHm
                : (_message.EventData.NewData["householdMembers"] as List<HouseholdMembers>).First();
            var person = CreatePerson(personId);
            person.Tenures.First().Id = _tenure.Id;
            _mockGateway.Setup(x => x.GetPersonByIdAsync(person.Id)).ReturnsAsync(person);

            _mockTenureApi.Setup(x => x.GetTenureInfoByIdAsync(_message.EntityId, _message.CorrelationId))
                                       .ReturnsAsync(_tenure);

            await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);

            _mockGateway.Verify(x => x.GetPersonByIdAsync(person.Id), Times.Once);
            _mockGateway.Verify(x => x.SavePersonAsync(It.Is<Person>(y => VerifyUpdatedPerson(y, _tenure, changedHm))),
                                Times.Once);
        }

        private bool VerifyUpdatedPerson(Person updatedPerson, TenureResponseObject tenure, HouseholdMembers changedHm)
        {
            var personTenure = updatedPerson.Tenures.FirstOrDefault(x => x.Id == tenure.Id);
            personTenure.Should().NotBeNull();

            personTenure.AssetFullAddress.Should().Be(tenure.TenuredAsset.FullAddress);
            personTenure.AssetId.Should().Be(tenure.TenuredAsset.Id.ToString());
            personTenure.EndDate.Should().Be(tenure.EndOfTenureDate?.ToFormattedDateTime());
            personTenure.PaymentReference.Should().Be(tenure.PaymentReference);
            // personTenure.PropertyReference.Should().Be(tenure.TenuredAsset.PropertyReference); // TODO...
            personTenure.StartDate.Should().Be(tenure.StartOfTenureDate.ToFormattedDateTime());
            personTenure.Type.Should().Be(tenure.TenureType.Description);
            personTenure.Uprn.Should().Be(tenure.TenuredAsset.Uprn);

            var personType = (PersonType) Enum.Parse(typeof(PersonType), changedHm.PersonTenureType);
            updatedPerson.PersonTypes.Should().Contain(personType);

            return true;
        }
    }
}
