using Hackney.Core.Logging;
using Hackney.Shared.Person;
using Hackney.Shared.Person.Domain;
using Hackney.Shared.Tenure.Boundary.Response;
using Hackney.Shared.Tenure.Domain;
using PersonListener.Boundary;
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
    public class PersonRemovedFromTenureUseCase : IPersonRemovedFromTenureUseCase
    {
        private readonly IDbPersonGateway _gateway;
        private readonly ITenureInfoApiGateway _tenureInfoApi;

        public PersonRemovedFromTenureUseCase(IDbPersonGateway gateway, ITenureInfoApiGateway tenureInfoApi)
        {
            _gateway = gateway;
            _tenureInfoApi = tenureInfoApi;
        }

        [LogCall]
        public async Task ProcessMessageAsync(EntityEventSns message)
        {
            if (message is null) throw new ArgumentNullException(nameof(message));


            // #2 - Get the deleted person...
            var householdMember = GetDeletedHouseholdMember(message.EventData);
            if (householdMember is null) throw new HouseholdMembersNotChangedException(message.EntityId, message.CorrelationId);

            var personId = householdMember.Id;

            Person person = await _gateway.GetPersonByIdAsync(personId).ConfigureAwait(false);
            if (person is null) throw new EntityNotFoundException<Person>(personId);

            // #3 - Remove the person with the tenure info
            var listPersonTenures = person.Tenures.ToList();
            if (person.Tenures.Any(x => x.Id == message.EntityId))
                listPersonTenures.Remove(person.Tenures.First(x => x.Id == message.EntityId));
            person.Tenures = listPersonTenures;

            //update the person.personType
            UpdatePersonType(person, message);

            // #4 - Save updated entity
            await _gateway.SavePersonAsync(person).ConfigureAwait(false);
        }

        private void UpdatePersonType(Person person, EntityEventSns message)
        {
            var getTenureFromIndexTasks = person.Tenures.Select(x => _tenureInfoApi.GetTenureInfoByIdAsync(x.Id, message.CorrelationId)).ToArray();
            Task.WaitAll(getTenureFromIndexTasks);

            var personTypes = getTenureFromIndexTasks.Select(x => GetPersonTypeForTenure(x.Result, person.Id)).ToList();
            person.PersonTypes = personTypes;
        }

        private PersonType GetPersonTypeForTenure(TenureResponseObject tenure, Guid personId)
        {
            var hm = tenure.HouseholdMembers.First(x => x.Id == personId);
            var tt = new TenureType()
            {
                Code = tenure.TenureType.Code,
                Description = tenure.TenureType.Description
            };
            var personTenureType = TenureTypes.GetPersonTenureType(tt, hm.IsResponsible);
            return (PersonType) Enum.Parse(typeof(PersonType), Enum.GetName(typeof(PersonTenureType), personTenureType));
        }

        private static HouseholdMembers GetDeletedHouseholdMember(EventData eventData)
        {
            var oldHms = GetHouseholdMembersFromEventData(eventData.OldData);
            var newHms = GetHouseholdMembersFromEventData(eventData.NewData);

            return oldHms.Except(newHms).FirstOrDefault();
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
