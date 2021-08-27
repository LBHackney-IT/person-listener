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

namespace PersonListener.Tests.UseCase
{
    [Collection("LogCall collection")]
    public class PersonAddedToTenureUseCaseTests
    {
        private readonly Mock<IDbPersonGateway> _mockGateway;
        private readonly PersonAddedToTenureUseCase _sut;
        private readonly Person _domainEntity;

        private readonly EntityEventSns _message;

        private readonly Fixture _fixture;

        public PersonAddedToTenureUseCaseTests()
        {
            _fixture = new Fixture();

            _mockGateway = new Mock<IDbPersonGateway>();
            _sut = new PersonAddedToTenureUseCase(_mockGateway.Object);

            _domainEntity = _fixture.Create<Person>();
            _message = CreateMessage(_domainEntity.Id);

            _mockGateway.Setup(x => x.GetPersonByIdAsync(_domainEntity.Id)).ReturnsAsync(_domainEntity);
        }

        private EntityEventSns CreateMessage(Guid id, string eventType = EventTypes.PersonAddedToTenureEvent)
        {
            return _fixture.Build<EntityEventSns>()
                           .With(x => x.EntityId, id)
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
        public void ProcessMessageAsyncTestEntityIdNotFoundThrows()
        {
            _mockGateway.Setup(x => x.GetPersonByIdAsync(_domainEntity.Id)).ReturnsAsync((Person) null);
            Func<Task> func = async () => await _sut.ProcessMessageAsync(null).ConfigureAwait(false);
            func.Should().ThrowAsync<EntityNotFoundException<Person>>();
        }

        [Fact]
        public void ProcessMessageAsyncTestSaveEntityThrows()
        {
            var exMsg = "This is the last error";
            _mockGateway.Setup(x => x.SavePersonAsync(It.IsAny<Person>()))
                        .ThrowsAsync(new Exception(exMsg));

            Func<Task> func = async () => await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);
            func.Should().ThrowAsync<Exception>().WithMessage(exMsg);

            _mockGateway.Verify(x => x.GetPersonByIdAsync(_domainEntity.Id), Times.Once);
            _mockGateway.Verify(x => x.SavePersonAsync(_domainEntity), Times.Once);
        }

        [Fact]
        public async Task ProcessMessageAsyncTestSaveEntitySucceeds()
        {
            await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);

            _mockGateway.Verify(x => x.GetPersonByIdAsync(_domainEntity.Id), Times.Once);
            _mockGateway.Verify(x => x.SavePersonAsync(_domainEntity), Times.Once);
        }
    }
}
