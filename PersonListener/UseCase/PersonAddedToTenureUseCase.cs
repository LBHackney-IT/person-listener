using Hackney.Core.Logging;
using PersonListener.Boundary;
using PersonListener.Domain;
using PersonListener.Domain.TenureInformation;
using PersonListener.Gateway.Interfaces;
using PersonListener.Infrastructure.Exceptions;
using PersonListener.UseCase.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PersonListener.UseCase
{
    public class PersonAddedToTenureUseCase : IPersonAddedToTenureUseCase
    {
        private readonly IDbPersonGateway _gateway;
        private readonly ITenureInfoApiGateway _tenureInfoApi;

        public PersonAddedToTenureUseCase(IDbPersonGateway gateway, ITenureInfoApiGateway tenureInfoApi)
        {
            _gateway = gateway;
            _tenureInfoApi = tenureInfoApi;
        }

        [LogCall]
        public async Task ProcessMessageAsync(EntityEventSns message)
        {
            if (message is null) throw new ArgumentNullException(nameof(message));

            // #1 - Get the tenure
            var tenure = await _tenureInfoApi.GetTenureInfoByIdAsync(message.EntityId)
                                             .ConfigureAwait(false);
            if (tenure is null) throw new EntityNotFoundException<TenureResponseObject>(message.EntityId);

            // #2 - Get the added person...
            // TODO - can proabably work this our from the EventData new and old info.
            var personId = tenure.HouseholdMembers.First().Id;

            Person person = await _gateway.GetPersonByIdAsync(personId).ConfigureAwait(false);
            if (person is null) throw new EntityNotFoundException<Person>(personId);

            // #3 - Update the person with the tenure info
            // TODO

            // #4 - Save updated entity
            await _gateway.SavePersonAsync(person).ConfigureAwait(false);
        }
    }
}
