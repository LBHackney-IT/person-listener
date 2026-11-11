using Hackney.Core.Logging;
using Hackney.Shared.Person;
using Hackney.Shared.Tenure.Boundary.Response;
using Hackney.Shared.Tenure.Domain;
using PersonListener.Boundary;
using PersonListener.Domain.Account;
using PersonListener.Gateway.Interfaces;
using PersonListener.Infrastructure.Exceptions;
using PersonListener.UseCase.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PersonListener.UseCase
{
    public class UpdateAccountDetailsOnPersonTenure : IUpdateAccountDetailsOnPersonTenure
    {
        private readonly IDbPersonGateway _gateway;
        private readonly IAccountApi _accountApi;
        private readonly ITenureInfoApiGateway _tenureInfoApi;

        public UpdateAccountDetailsOnPersonTenure(IDbPersonGateway gateway, IAccountApi accountApi,
            ITenureInfoApiGateway tenureInfoApi)
        {
            _gateway = gateway;
            _accountApi = accountApi;
            _tenureInfoApi = tenureInfoApi;
        }

        [LogCall]
        public async Task ProcessMessageAsync(EntityEventSns message)
        {
            if (message is null) throw new ArgumentNullException(nameof(message));

            // #1 - Get the account
            var account = await _accountApi.GetAccountByIdAsync(message.EntityId, message.CorrelationId)
                                             .ConfigureAwait(false);
            if (account is null) throw new EntityNotFoundException<AccountResponseObject>(message.EntityId);

            // #2 - Get the tenure
            var tenure = await _tenureInfoApi.GetTenureInfoByIdAsync(account.TargetId, message.CorrelationId)
                                             .ConfigureAwait(false);
            if (tenure is null) throw new EntityNotFoundException<TenureResponseObject>(account.TargetId);

            // #3 - If there are no Household members we have nothing to do.
            if (tenure.HouseholdMembers is null)
                return;

            // #4 - For each Household member update the tenure payment ref on the person record
            // with the value from the account
            var updatedPersons = new List<Person>();
            var exceptions = new List<Exception>();
            foreach (var hm in tenure.HouseholdMembers)
            {
                try
                {
                    updatedPersons.Add(await GetAndUpdatePersonRecord(tenure, account, hm).ConfigureAwait(false));
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            if (exceptions.Any())
                throw new AggregateException(exceptions);

            foreach (var p in updatedPersons)
                await _gateway.SavePersonAsync(p).ConfigureAwait(false);
        }

        private async Task<Person> GetAndUpdatePersonRecord(TenureResponseObject tenure, AccountResponseObject account,
            HouseholdMembers hm)
        {
            var thisPerson = await _gateway.GetPersonByIdAsync(hm.Id).ConfigureAwait(false);
            if (thisPerson is null) throw new EntityNotFoundException<Person>(hm.Id);

            var personTenure = thisPerson.Tenures.FirstOrDefault(x => x.Id == tenure.Id);
            if (personTenure is null) throw new PersonMissingTenureException(thisPerson.Id, tenure.Id);

            personTenure.PaymentReference = account.PaymentReference;
            return thisPerson;
        }
    }
}
