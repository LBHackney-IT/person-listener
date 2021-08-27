using Amazon.DynamoDBv2.DataModel;
using FluentAssertions;
using PersonListener.Domain;
using PersonListener.Domain.TenureInformation;
using PersonListener.Infrastructure;
using PersonListener.Infrastructure.Exceptions;
using System;
using System.Threading.Tasks;

namespace PersonListener.Tests.E2ETests.Steps
{
    public class PersonAddedToTenureUseCaseSteps : BaseSteps
    {
        public PersonAddedToTenureUseCaseSteps()
        { }

        public async Task WhenTheFunctionIsTriggered(Guid id)
        {
            await TriggerFunction(id).ConfigureAwait(false);
        }

        public async Task ThenThePersonIsUpdated(PersonDbEntity beforeChange,
            TenureResponseObject tenure, IDynamoDBContext dbContext)
        {
            var entityInDb = await dbContext.LoadAsync<PersonDbEntity>(beforeChange.Id);

            // TODO - update this... to validate against the details from the tenure
            entityInDb.Should().BeEquivalentTo(beforeChange,
                config => config/*.Excluding(y => y.Description)*/
                                .Excluding(d => d.LastModified)
                                .Excluding(z => z.VersionNumber));
            //entityInDb.Description.Should().Be("Updated");
            entityInDb.LastModified.Should().BeCloseTo(DateTime.UtcNow, 100);
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
    }
}
