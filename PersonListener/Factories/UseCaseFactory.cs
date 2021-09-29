using Microsoft.Extensions.DependencyInjection;
using PersonListener.Boundary;
using PersonListener.UseCase.Interfaces;
using System;

namespace PersonListener.Factories
{
    public static class UseCaseFactory
    {
        public static IMessageProcessing CreateUseCaseForMessage(this EntityEventSns entityEvent, IServiceProvider serviceProvider)
        {
            if (entityEvent is null) throw new ArgumentNullException(nameof(entityEvent));
            if (serviceProvider is null) throw new ArgumentNullException(nameof(serviceProvider));

            IMessageProcessing processor = null;
            switch (entityEvent.EventType)
            {
                case EventTypes.PersonAddedToTenureEvent:
                    {
                        processor = serviceProvider.GetService<IPersonAddedToTenureUseCase>();
                        break;
                    }
                case EventTypes.TenureUpdatedEvent:
                    {
                        processor = serviceProvider.GetService<ITenureUpdatedUseCase>();
                        break;
                    }
                case EventTypes.PersonRemovedFromTenureEvent:
                    {
                        processor = serviceProvider.GetService<IPersonRemovedFromTenureUseCase>();
                        break;
                    }
                // We can ignore these and just let them go as we don't care about them
                case EventTypes.TenureCreatedEvent:
                    break;

                default:
                    throw new ArgumentException($"Unknown event type: {entityEvent.EventType}");
            }

            return processor;
        }
    }
}
