using AutoFixture;
using PersonListener.Boundary;
using PersonListener.Domain;
using PersonListener.Gateway.Interfaces;
using PersonListener.Infrastructure.Exceptions;
using PersonListener.UseCase;
using FluentAssertions;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;
using PersonListener.Domain.TenureInformation;
using System.Linq;

namespace PersonListener.Tests.UseCase
{
    [Collection("LogCall collection")]
    public class PersonAddedToTenureUseCaseTests
    {
        private readonly Mock<IDbPersonGateway> _mockGateway;
        private readonly Mock<ITenureInfoApiGateway> _mockTenureApi;
        private readonly PersonAddedToTenureUseCase _sut;
        private readonly Person _person;

        private readonly EntityEventSns _message;
        private readonly TenureResponseObject _tenure;

        private readonly Fixture _fixture;

        public PersonAddedToTenureUseCaseTests()
        {
            _fixture = new Fixture();

            _mockGateway = new Mock<IDbPersonGateway>();
            _mockTenureApi = new Mock<ITenureInfoApiGateway>();
            _sut = new PersonAddedToTenureUseCase(_mockGateway.Object, _mockTenureApi.Object);

            _message = CreateMessage();
            _tenure = CreateTenure(_message.EntityId);
            _person = CreatePerson(_tenure.HouseholdMembers.First().Id);

            _mockGateway.Setup(x => x.GetPersonByIdAsync(_person.Id)).ReturnsAsync(_person);
        }

        private TenureResponseObject CreateTenure(Guid entityId)
        {
            return _fixture.Build<TenureResponseObject>()
                           .With(x => x.Id, entityId)
                           .Create();
        }

        private Person CreatePerson(Guid entityId)
        {
            return _fixture.Build<Person>()
                           .With(x => x.Id, entityId)
                           .Create();
        }

        private EntityEventSns CreateMessage(string eventType = EventTypes.PersonAddedToTenureEvent)
        {
            return _fixture.Build<EntityEventSns>()
                           .With(x => x.EventType, eventType)
                           .Create();
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
            _mockTenureApi.Setup(x => x.GetTenureInfoByIdAsync(_message.EntityId))
                                       .ThrowsAsync(new Exception(exMsg));

            Func<Task> func = async () => await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);
            func.Should().ThrowAsync<Exception>().WithMessage(exMsg);
        }

        [Fact]
        public void ProcessMessageAsyncTestGetTenureReturnsNullThrows()
        {
            _mockTenureApi.Setup(x => x.GetTenureInfoByIdAsync(_message.EntityId))
                                       .ReturnsAsync((TenureResponseObject) null);

            Func<Task> func = async () => await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);
            func.Should().ThrowAsync<EntityNotFoundException<TenureResponseObject>>();
        }

        [Fact]
        public void ProcessMessageAsyncTestPersonIdNotFoundThrows()
        {
            _mockTenureApi.Setup(x => x.GetTenureInfoByIdAsync(_message.EntityId))
                                       .ReturnsAsync(_tenure);
            _mockGateway.Setup(x => x.GetPersonByIdAsync(_person.Id)).ReturnsAsync((Person) null);
            Func<Task> func = async () => await _sut.ProcessMessageAsync(null).ConfigureAwait(false);
            func.Should().ThrowAsync<EntityNotFoundException<Person>>();
        }

        [Fact]
        public void ProcessMessageAsyncTestSaveEntityThrows()
        {
            _mockTenureApi.Setup(x => x.GetTenureInfoByIdAsync(_message.EntityId))
                                       .ReturnsAsync(_tenure);
            var exMsg = "This is the last error";
            _mockGateway.Setup(x => x.SavePersonAsync(It.IsAny<Person>()))
                        .ThrowsAsync(new Exception(exMsg));

            Func<Task> func = async () => await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);
            func.Should().ThrowAsync<Exception>().WithMessage(exMsg);

            _mockGateway.Verify(x => x.GetPersonByIdAsync(_person.Id), Times.Once);
            _mockGateway.Verify(x => x.SavePersonAsync(_person), Times.Once);
        }

        [Fact]
        public async Task ProcessMessageAsyncTestSaveEntitySucceeds()
        {
            _mockTenureApi.Setup(x => x.GetTenureInfoByIdAsync(_message.EntityId))
                                       .ReturnsAsync(_tenure);

            await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);

            _mockGateway.Verify(x => x.GetPersonByIdAsync(_person.Id), Times.Once);
            _mockGateway.Verify(x => x.SavePersonAsync(It.Is<Person>(y => VerifyUpdatedPerson(y, _tenure))),
                                Times.Once);
        }

        private bool VerifyUpdatedPerson(Person updatedPerson, TenureResponseObject tenure)
        {
            // TODO - verify all expected data...

            return true;
        }
    }
}
