using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.TestUtilities;
using AutoFixture;
using FluentAssertions;
using Hackney.Shared.Person;
using Hackney.Shared.Person.Domain;
using Hackney.Shared.Person.Infrastructure;
using Moq;
using PersonListener.Boundary;
using PersonListener.Infrastructure.Exceptions;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace PersonListener.Tests.E2ETests.Steps
{
    public class PersonRemovedFromTenureStep : BaseSteps
    {
        private new readonly Fixture _fixture = new Fixture();
        private new Exception _lastException;
        protected new readonly Guid _correlationId = Guid.NewGuid();

        public PersonRemovedFromTenureStep()
        { }

        private SQSEvent.SQSMessage CreateMessage(Guid personId, EventData eventData, string eventType = EventTypes.PersonRemovedFromTenureEvent)
        {
            var personSns = _fixture.Build<EntityEventSns>()
                                    .With(x => x.EntityId, personId)
                                    .With(x => x.EventType, eventType)
                                    .With(x => x.EventData, eventData)
                                    .With(x => x.CorrelationId, _correlationId)
                                    .Create();

            var msgBody = JsonSerializer.Serialize(personSns, _jsonOptions);
            return _fixture.Build<SQSEvent.SQSMessage>()
                           .With(x => x.Body, msgBody)
                           .With(x => x.MessageAttributes, new Dictionary<string, SQSEvent.MessageAttribute>())
                           .Create();
        }

        public async Task WhenTheFunctionIsTriggered(Guid tenureId, EventData eventData, string eventType)
        {
            var mockLambdaLogger = new Mock<ILambdaLogger>();
            ILambdaContext lambdaContext = new TestLambdaContext()
            {
                Logger = mockLambdaLogger.Object
            };



            var msg = CreateMessage(tenureId, eventData, eventType);
            var sqsEvent = _fixture.Build<SQSEvent>()
                                   .With(x => x.Records, new List<SQSEvent.SQSMessage> { msg })
                                   .Create();

            Func<Task> func = async () =>
            {
                var fn = new SqsFunction();
                await fn.FunctionHandler(sqsEvent, lambdaContext).ConfigureAwait(false);
            };

            _lastException = await Record.ExceptionAsync(func);

        }

        public void ThenTheCorrelationIdWasUsedInTheApiCall(List<string> receivedCorrelationIds)
        {
            receivedCorrelationIds.Should().Contain(_correlationId.ToString());
        }

        public void ThenAPersonNotFoundExceptionIsThrown(Guid id)
        {
            _lastException.Should().NotBeNull();
            _lastException.Should().BeOfType(typeof(EntityNotFoundException<Person>));
            (_lastException as EntityNotFoundException<Person>).Id.Should().Be(id);
        }

        public async Task ThenTheDatabaseIsUpdatedWithThePerson(Person person, Guid tenureId, IDynamoDBContext dbContext)
        {
            var result = await dbContext.LoadAsync<PersonDbEntity>(person.Id)
                                       .ConfigureAwait(false);

            result.Should().BeEquivalentTo(person, c => c.Excluding(y => y.Tenures));
            result.Tenures.Should().NotContain(x => x.Id == tenureId);
        }

        public async Task ThenThePersonHasTheTenureRemoved(Guid personId, Guid tenureId, IDynamoDBContext dbContext)
        {
            var result = await dbContext.LoadAsync<PersonDbEntity>(personId)
                                       .ConfigureAwait(false);


            result.Tenures.Should().NotContain(x => x.Id == tenureId);
            result.PersonTypes.Should().NotContain(PersonType.Freeholder);
        }
    }
}
