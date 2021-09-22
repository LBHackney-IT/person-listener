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
using System.Threading.Tasks;


namespace PersonListener.UseCase
{
    public class TenureUpdatedUseCase : ITenureUpdatedUseCase
    {
        private readonly IDbPersonGateway _gateway;
        private readonly ITenureInfoApiGateway _tenureInfoApi;

        public TenureUpdatedUseCase(IDbPersonGateway gateway, ITenureInfoApiGateway tenureInfoApi)
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

            // #2 - If there are no Household members we have nothing to do.
            if (tenure.HouseholdMembers is null)
                return;

            // #3 - For each Household member update the tenure info on the person record
            var tasks = new List<Task>();
            var updatedPersons = new List<Person>();
            foreach (var hm in tenure.HouseholdMembers)
                tasks.Add(UpdatePersonRecord(tenure, hm, updatedPersons));
            Task.WaitAll(tasks.ToArray());

            // #4 - Save all updated person records
            tasks.Clear();
            foreach (var p in updatedPersons)
                tasks.Add(_gateway.SavePersonAsync(p));

            Task.WaitAll(tasks.ToArray());
        }

        private async Task UpdatePersonRecord(TenureResponseObject tenure, HouseholdMembers hm, List<Person> updatedRecords)
        {
            var thisPerson = await _gateway.GetPersonByIdAsync(hm.Id).ConfigureAwait(false);
            if (thisPerson is null) throw new EntityNotFoundException<Person>(hm.Id);

            var personTenure = thisPerson.Tenures.FirstOrDefault(x => x.Id == tenure.Id);
            if (personTenure is null) throw new PersonMissingTenureException(thisPerson.Id, tenure.Id);

            personTenure.AssetFullAddress = tenure.TenuredAsset.FullAddress;
            personTenure.AssetId = tenure.TenuredAsset.Id.ToString();
            personTenure.EndDate = tenure.EndOfTenureDate?.ToFormattedDateTime();
            personTenure.PaymentReference = tenure.PaymentReference;
            // personTenure.PropertyReference = tenure.PropertyReference; // TODO - property not yet available
            personTenure.StartDate = tenure.StartOfTenureDate.ToFormattedDateTime();
            personTenure.Type = tenure.TenureType.Description;
            personTenure.Uprn = tenure.TenuredAsset.Uprn;

            updatedRecords.Add(thisPerson);
        }
    }
}
