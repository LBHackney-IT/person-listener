using Hackney.Core.Logging;
using PersonListener.Boundary;
using PersonListener.Domain;
using PersonListener.Gateway.Interfaces;
using PersonListener.Infrastructure.Exceptions;
using PersonListener.UseCase.Interfaces;
using System;
using System.Threading.Tasks;

namespace PersonListener.UseCase
{
    public class PersonAddedToTenureUseCase : IPersonAddedToTenureUseCase
    {
        private readonly IDbPersonGateway _gateway;

        public PersonAddedToTenureUseCase(IDbPersonGateway gateway)
        {
            _gateway = gateway;
        }

        [LogCall]
        public async Task ProcessMessageAsync(EntityEventSns message)
        {
            if (message is null) throw new ArgumentNullException(nameof(message));

            // #1 - Get the tenure
            // TODO - will need a Tenure Gateway to call the Tenure API

            // #2 - Get the person...
            Person person = await _gateway.GetPersonByIdAsync(message.EntityId).ConfigureAwait(false);
            if (person is null) throw new EntityNotFoundException<Person>(message.EntityId);

            // #3 - Update the person with the tenure info
            // TODO

            // #4 - Save updated entity
            await _gateway.SavePersonAsync(person).ConfigureAwait(false);
        }
    }
}
