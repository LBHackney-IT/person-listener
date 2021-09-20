using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.TestUtilities;
using AutoFixture;
using PersonListener.Boundary;
using PersonListener.Infrastructure;
using Moq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace PersonListener.Tests.E2ETests.Steps
{
    public class BaseSteps
    {
        protected readonly JsonSerializerOptions _jsonOptions = JsonOptions.CreateJsonOptions();
        protected readonly Fixture _fixture = new Fixture();
        protected Exception _lastException;
        protected string _eventType;

        public BaseSteps()
        { }

        protected EntityEventSns CreateEvent(Guid personId, string eventType)
        {
            return _fixture.Build<EntityEventSns>()
                           .With(x => x.EntityId, personId)
                           .With(x => x.EventType, _eventType)
                           .Create();
        }

        protected SQSEvent.SQSMessage CreateMessage(Guid personId)
        {
            return CreateMessage(CreateEvent(personId, _eventType));
        }

        protected SQSEvent.SQSMessage CreateMessage(EntityEventSns eventSns)
        {
            var msgBody = JsonSerializer.Serialize(eventSns, _jsonOptions);
            return _fixture.Build<SQSEvent.SQSMessage>()
                           .With(x => x.Body, msgBody)
                           .With(x => x.MessageAttributes, new Dictionary<string, SQSEvent.MessageAttribute>())
                           .Create();
        }

        protected async Task TriggerFunction(Guid id)
        {
            await TriggerFunction(CreateMessage(id)).ConfigureAwait(false);
        }

        protected async Task TriggerFunction(SQSEvent.SQSMessage message)
        {
            var mockLambdaLogger = new Mock<ILambdaLogger>();
            ILambdaContext lambdaContext = new TestLambdaContext()
            {
                Logger = mockLambdaLogger.Object
            };

            var sqsEvent = _fixture.Build<SQSEvent>()
                                   .With(x => x.Records, new List<SQSEvent.SQSMessage> { message })
                                   .Create();

            Func<Task> func = async () =>
            {
                var fn = new SqsFunction();
                await fn.FunctionHandler(sqsEvent, lambdaContext).ConfigureAwait(false);
            };

            _lastException = await Record.ExceptionAsync(func);
        }
    }
}
