using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.SQSEvents;
using FluentAssertions;
using Force.DeepCloner;
using PersonListener.Boundary;
using PersonListener.Domain;
using PersonListener.Domain.TenureInformation;
using PersonListener.Infrastructure;
using PersonListener.Infrastructure.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PersonListener.Tests.E2ETests.Steps
{
    public class PersonAddedToTenureUseCaseSteps : BaseSteps
    {
        public SQSEvent.SQSMessage TheMessage { get; private set; }
        public Guid NewPersonId { get; private set; }

        public PersonAddedToTenureUseCaseSteps()
        { }

        public void GivenAMessageWithNoPersonAdded(TenureResponseObject tenure)
        {
            var eventSns = CreateEvent(tenure.Id);
            var newData = tenure.HouseholdMembers;
            var oldData = newData.DeepClone();
            eventSns.EventData = new EventData()
            {
                OldData = new Dictionary<string, object> { { "HouseholdMembers", oldData } },
                NewData = new Dictionary<string, object> { { "HouseholdMembers", newData } }
            };
            TheMessage = CreateMessage(eventSns);
        }

        public void GivenAMessageWithPersonAdded(TenureResponseObject tenure)
        {
            var eventSns = CreateEvent(tenure.Id);
            var newData = tenure.HouseholdMembers;
            var oldData = newData.DeepClone().Take(newData.Count - 1).ToList();
            eventSns.EventData = new EventData()
            {
                OldData = new Dictionary<string, object> { { "HouseholdMembers", oldData } },
                NewData = new Dictionary<string, object> { { "HouseholdMembers", newData } }
            };
            TheMessage = CreateMessage(eventSns);
            NewPersonId = newData.Last().Id;
        }

        public async Task WhenTheFunctionIsTriggered(Guid id)
        {
            await TriggerFunction(id).ConfigureAwait(false);
        }

        public async Task WhenTheFunctionIsTriggered(SQSEvent.SQSMessage message)
        {
            await TriggerFunction(message).ConfigureAwait(false);
        }

        public async Task ThenThePersonIsUpdated(PersonDbEntity beforeChange,
            TenureResponseObject tenure, IDynamoDBContext dbContext)
        {
            var entityInDb = await dbContext.LoadAsync<PersonDbEntity>(beforeChange.Id);

            entityInDb.Should().BeEquivalentTo(beforeChange,
                config => config.Excluding(y => y.Tenures)
                                .Excluding(d => d.PersonTypes)
                                .Excluding(d => d.LastModified)
                                .Excluding(z => z.VersionNumber));

            var newTenure = entityInDb.Tenures.FirstOrDefault(x => x.Id == tenure.Id);
            newTenure.Should().NotBeNull();
            newTenure.AssetFullAddress.Should().Be(tenure.TenuredAsset.FullAddress);
            newTenure.AssetId.Should().Be(tenure.TenuredAsset.Id.ToString());
            newTenure.EndDate.Should().Be(tenure.EndOfTenureDate?.ToString());
            newTenure.PaymentReference.Should().Be(tenure.PaymentReference);
            // newTenure.PropertyReference.Should().Be(tenure.TenuredAsset.PropertyReference); // TODO...
            newTenure.StartDate.Should().Be(tenure.StartOfTenureDate.ToString());
            newTenure.Type.Should().Be(tenure.TenureType.Description);
            newTenure.Uprn.Should().Be(tenure.TenuredAsset.Uprn);

            entityInDb.PersonTypes.Should().Contain(PersonType.Occupant);

            entityInDb.LastModified.Should().BeCloseTo(DateTime.UtcNow, 1000);
            entityInDb.VersionNumber.Should().Be(beforeChange.VersionNumber + 1);
        }

        public void ThenATenureNotFoundExceptionIsThrown(Guid id)
        {
            _lastException.Should().NotBeNull();
            _lastException.Should().BeOfType(typeof(EntityNotFoundException<TenureResponseObject>));
            (_lastException as EntityNotFoundException<TenureResponseObject>).Id.Should().Be(id);
        }

        public void ThenAPersonNotFoundExceptionIsThrown(Guid id)
        {
            _lastException.Should().NotBeNull();
            _lastException.Should().BeOfType(typeof(EntityNotFoundException<Person>));
            (_lastException as EntityNotFoundException<Person>).Id.Should().Be(id);
        }

        public void ThenAHouseholdMembersNotChangedExceptionIsThrown(Guid tenureId)
        {
            _lastException.Should().NotBeNull();
            _lastException.Should().BeOfType(typeof(HouseholdMembersNotChangedException));
            (_lastException as HouseholdMembersNotChangedException).TenureId.Should().Be(tenureId);
        }
    }
}
