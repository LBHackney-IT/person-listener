using Amazon.DynamoDBv2.DataModel;
using PersonListener.Domain;
using PersonListener.Infrastructure;
using PersonListener.Infrastructure.Exceptions;
using FluentAssertions;
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

        public async Task ThenTheEntityIsUpdated(PersonDbEntity beforeChange, IDynamoDBContext dbContext)
        {
            var entityInDb = await dbContext.LoadAsync<PersonDbEntity>(beforeChange.Id);

            // TODO - update this...
            entityInDb.Should().BeEquivalentTo(beforeChange,
                config => config/*.Excluding(y => y.Description)*/
                                .Excluding(d => d.LastModified)
                                .Excluding(z => z.VersionNumber));
            //entityInDb.Description.Should().Be("Updated");
            entityInDb.LastModified.Should().BeCloseTo(DateTime.UtcNow, 100);
            entityInDb.VersionNumber.Should().Be(beforeChange.VersionNumber + 1);
        }

        public void ThenAnEntityNotFoundExceptionIsThrown(Guid id)
        {
            _lastException.Should().NotBeNull();
            _lastException.Should().BeOfType(typeof(EntityNotFoundException<Person>));
            (_lastException as EntityNotFoundException<Person>).Id.Should().Be(id);
        }
    }
}
