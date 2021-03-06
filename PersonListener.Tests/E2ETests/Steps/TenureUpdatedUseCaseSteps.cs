using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.SQSEvents;
using FluentAssertions;
using Hackney.Shared.Person;
using Hackney.Shared.Person.Infrastructure;
using Hackney.Shared.Tenure.Boundary.Response;
using PersonListener.Infrastructure;
using PersonListener.Infrastructure.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace PersonListener.Tests.E2ETests.Steps
{
    public class TenureUpdatedUseCaseSteps : BaseSteps
    {
        public SQSEvent.SQSMessage TheMessage { get; private set; }
        public Guid NewPersonId { get; private set; }

        public TenureUpdatedUseCaseSteps()
        {
            _eventType = EventTypes.TenureUpdatedEvent;
        }

        public async Task WhenTheFunctionIsTriggered(Guid id)
        {
            await TriggerFunction(id).ConfigureAwait(false);
        }

        public async Task WhenTheFunctionIsTriggered(SQSEvent.SQSMessage message)
        {
            await TriggerFunction(message).ConfigureAwait(false);
        }

        public void TheNoExceptionIsThrown()
        {
            _lastException.Should().BeNull();
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

        public async Task ThenThePersonsAreUpdated(List<PersonDbEntity> persons, TenureResponseObject tenure, IDynamoDBContext dbContext)
        {
            foreach (var hm in tenure.HouseholdMembers)
            {
                var beforeChangePerson = persons.First(x => x.Id == hm.Id);
                var updatedPersonInDb = await dbContext.LoadAsync<PersonDbEntity>(beforeChangePerson.Id);

                updatedPersonInDb.Should().BeEquivalentTo(beforeChangePerson,
                    config => config.Excluding(y => y.Tenures)
                                    .Excluding(d => d.LastModified)
                                    .Excluding(z => z.VersionNumber));
                updatedPersonInDb.Tenures.Where(x => x.Id != tenure.Id).Should().BeEquivalentTo(
                    beforeChangePerson.Tenures.Where(x => x.Id != tenure.Id));

                var updatedTenure = updatedPersonInDb.Tenures.First(x => x.Id == tenure.Id);
                updatedTenure.AssetFullAddress.Should().Be(tenure.TenuredAsset.FullAddress);
                updatedTenure.AssetId.Should().Be(tenure.TenuredAsset.Id.ToString());
                updatedTenure.EndDate.Should().Be(tenure.EndOfTenureDate?.ToFormattedDateTime());
                updatedTenure.PaymentReference.Should().Be(tenure.PaymentReference);
                // newTenure.PropertyReference.Should().Be(tenure.TenuredAsset.PropertyReference); // TODO...
                updatedTenure.StartDate.Should().Be(tenure.StartOfTenureDate?.ToFormattedDateTime());
                updatedTenure.Type.Should().Be(tenure.TenureType.Description);
                updatedTenure.Uprn.Should().Be(tenure.TenuredAsset.Uprn);

                updatedPersonInDb.LastModified.Should().BeCloseTo(DateTime.UtcNow, 1000);
                updatedPersonInDb.VersionNumber.Should().Be(beforeChangePerson.VersionNumber + 1);
            }
        }
    }
}
