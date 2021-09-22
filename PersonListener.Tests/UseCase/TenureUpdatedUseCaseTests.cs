using AutoFixture;
using FluentAssertions;
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
    public class TenureUpdatedUseCaseTests
    {
        private readonly Mock<IDbPersonGateway> _mockGateway;
        private readonly Mock<ITenureInfoApiGateway> _mockTenureApi;
        private readonly TenureUpdatedUseCase _sut;

        private readonly EntityEventSns _message;
        private readonly TenureResponseObject _tenure;

        private readonly Fixture _fixture;
        private static readonly Guid _correlationId = Guid.NewGuid();

        public TenureUpdatedUseCaseTests()
        {
            _fixture = new Fixture();

            _mockGateway = new Mock<IDbPersonGateway>();
            _mockTenureApi = new Mock<ITenureInfoApiGateway>();
            _sut = new TenureUpdatedUseCase(_mockGateway.Object, _mockTenureApi.Object);

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
            _mockTenureApi.Setup(x => x.GetTenureInfoByIdAsync(_message.EntityId, _message.CorrelationId))
                                       .ReturnsAsync(_tenure);
            await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);

            _mockGateway.Verify(x => x.GetPersonByIdAsync(It.IsAny<Guid>()), Times.Never());
            _mockGateway.Verify(x => x.SavePersonAsync(It.IsAny<Person>()), Times.Never());
        }

        [Fact]
        public async Task ProcessMessageAsyncTestEmptyHouseholdMembersDoesNothing()
        {
            _tenure.HouseholdMembers.Clear();
            _mockTenureApi.Setup(x => x.GetTenureInfoByIdAsync(_message.EntityId, _message.CorrelationId))
                                       .ReturnsAsync(_tenure);
            await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);

            _mockGateway.Verify(x => x.GetPersonByIdAsync(It.IsAny<Guid>()), Times.Never());
            _mockGateway.Verify(x => x.SavePersonAsync(It.IsAny<Person>()), Times.Never());
        }

        [Fact]
        public void ProcessMessageAsyncTestMissingPersonThrows()
        {
            _mockTenureApi.Setup(x => x.GetTenureInfoByIdAsync(_message.EntityId, _message.CorrelationId))
                                       .ReturnsAsync(_tenure);
            Func<Task> func = async () => await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);
            func.Should().Throw<EntityNotFoundException<Person>>();

            _mockGateway.Verify(x => x.GetPersonByIdAsync(It.IsAny<Guid>()), Times.Exactly(3));
            _mockGateway.Verify(x => x.SavePersonAsync(It.IsAny<Person>()), Times.Never());
        }

        [Fact]
        public void ProcessMessageAsyncTestMissingPersonTenureThrows()
        {
            _mockTenureApi.Setup(x => x.GetTenureInfoByIdAsync(_message.EntityId, _message.CorrelationId))
                                       .ReturnsAsync(_tenure);
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
            _mockTenureApi.Setup(x => x.GetTenureInfoByIdAsync(_message.EntityId, _message.CorrelationId))
                                       .ReturnsAsync(_tenure);
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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ProcessMessageAsyncTestAllPersonsUpdated(bool nullEndDate)
        {
            if (nullEndDate) _tenure.EndOfTenureDate = null;

            _mockTenureApi.Setup(x => x.GetTenureInfoByIdAsync(_message.EntityId, _message.CorrelationId))
                                       .ReturnsAsync(_tenure);
            var persons = SetupPersonTenures();

            await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);

            foreach (var p in persons)
            {
                _mockGateway.Verify(x => x.GetPersonByIdAsync(p.Id), Times.Once());
                _mockGateway.Verify(x => x.SavePersonAsync(It.Is<Person>(
                                                y => y.Id == p.Id && VerifyPersonTenureUpdated(y, _tenure))),
                                    Times.Once());
            }
        }

        private bool VerifyPersonTenureUpdated(Person p, TenureResponseObject tenure)
        {
            var pt = p.Tenures.First(x => x.Id == tenure.Id);

            pt.AssetFullAddress.Should().Be(tenure.TenuredAsset.FullAddress);
            pt.AssetId.Should().Be(tenure.TenuredAsset.Id.ToString());
            pt.EndDate.Should().Be(tenure.EndOfTenureDate?.ToFormattedDateTime());
            pt.PaymentReference.Should().Be(tenure.PaymentReference);
            // pt.PropertyReference.Should().Be(tenure.PropertyReference); // TODO - property not yet available
            pt.StartDate.Should().Be(tenure.StartOfTenureDate.ToFormattedDateTime());
            pt.Type.Should().Be(tenure.TenureType.Description);
            pt.Uprn.Should().Be(tenure.TenuredAsset.Uprn);

            return true;
        }
    }
}
