using Hackney.Core.Logging;
using PersonListener.Boundary;
using PersonListener.Domain;
using PersonListener.Domain.TenureInformation;
using PersonListener.Gateway.Interfaces;
using PersonListener.Infrastructure;
using PersonListener.Infrastructure.Exceptions;
using PersonListener.UseCase.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
            var tenure = await _tenureInfoApi.GetTenureInfoByIdAsync(message.EntityId, message.CorrelationId)
                                             .ConfigureAwait(false);
            if (tenure is null) throw new EntityNotFoundException<TenureResponseObject>(message.EntityId);

            // #2 - Get the added person...
            var householdMember = GetAddedOrUpdatedHouseholdMember(message.EventData);
            if (householdMember is null) throw new HouseholdMembersNotChangedException(message.EntityId, message.CorrelationId);

            var personId = householdMember.Id;
            var personTenureType = (PersonType) Enum.Parse(typeof(PersonType), householdMember.PersonTenureType);

            Person person = await _gateway.GetPersonByIdAsync(personId).ConfigureAwait(false);
            if (person is null) throw new EntityNotFoundException<Person>(personId);

            // #3 - Update the person with the tenure info
            List<Tenure> tenures = (person.Tenures is null) ? new List<Tenure>() : new List<Tenure>(person.Tenures);
            var personTenure = tenures.FirstOrDefault(x => x.Id == tenure.Id);
            if (personTenure is null)
            {
                personTenure = new Tenure() { Id = tenure.Id };
                tenures.Add(personTenure);
            }

            personTenure.AssetFullAddress = tenure.TenuredAsset.FullAddress;
            personTenure.AssetId = tenure.TenuredAsset.Id.ToString();
            personTenure.EndDate = tenure.EndOfTenureDate?.ToFormattedDateTime();
            personTenure.PaymentReference = tenure.PaymentReference;
            //personTenure.PropertyReference = ???; // TODO - ignore for now...
            personTenure.StartDate = tenure.StartOfTenureDate.ToFormattedDateTime();
            personTenure.Type = tenure.TenureType.Description;
            personTenure.Uprn = tenure.TenuredAsset.Uprn;

            person.Tenures = tenures;

            // Also update the person types list if necessary
            if (!person.PersonTypes.Contains(personTenureType))
            {
                var pTypes = new List<PersonType>(person.PersonTypes);
                pTypes.Add(personTenureType);
                person.PersonTypes = pTypes;
            }

            // #4 - Save updated entity
            await _gateway.SavePersonAsync(person).ConfigureAwait(false);
        }

        private static HouseholdMembers GetAddedOrUpdatedHouseholdMember(EventData eventData)
        {
            var oldHms = GetHouseholdMembersFromEventData(eventData.OldData);
            var newHms = GetHouseholdMembersFromEventData(eventData.NewData);

            return newHms.Except(oldHms).FirstOrDefault();
        }

        private static List<HouseholdMembers> GetHouseholdMembersFromEventData(object data)
        {
            var dataDic = (data is Dictionary<string, object>) ? data as Dictionary<string, object> : ConvertFromObject<Dictionary<string, object>>(data);
            var hmsObj = dataDic["householdMembers"];
            return (hmsObj is List<HouseholdMembers>) ? hmsObj as List<HouseholdMembers> : ConvertFromObject<List<HouseholdMembers>>(hmsObj);
        }

        private static T ConvertFromObject<T>(object obj) where T : class
        {
            return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(obj), JsonOptions.CreateJsonOptions());
        }
    }
}
