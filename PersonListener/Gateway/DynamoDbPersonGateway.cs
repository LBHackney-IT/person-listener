using Amazon.DynamoDBv2.DataModel;
using Hackney.Core.Logging;
using Microsoft.Extensions.Logging;
using Hackney.Shared.Person;
using PersonListener.Factories;
using PersonListener.Gateway.Interfaces;
using PersonListener.Infrastructure;
using System;
using System.Threading.Tasks;

namespace PersonListener.Gateway
{
    public class DynamoDbPersonGateway : IDbPersonGateway
    {
        private readonly IDynamoDBContext _dynamoDbContext;
        private readonly ILogger<DynamoDbPersonGateway> _logger;

        public DynamoDbPersonGateway(IDynamoDBContext dynamoDbContext, ILogger<DynamoDbPersonGateway> logger)
        {
            _logger = logger;
            _dynamoDbContext = dynamoDbContext;
        }

        [LogCall]
        public async Task<Person> GetPersonByIdAsync(Guid id)
        {
            _logger.LogDebug($"Calling IDynamoDBContext.LoadAsync for id {id}");
            var dbEntity = await _dynamoDbContext.LoadAsync<PersonDbEntity>(id).ConfigureAwait(false);
            return dbEntity?.ToDomain();
        }

        [LogCall]
        public async Task SavePersonAsync(Person person)
        {
            person.LastModified = DateTime.UtcNow;
            _logger.LogDebug($"Calling IDynamoDBContext.SaveAsync for id {person.Id}");
            await _dynamoDbContext.SaveAsync(person.ToDatabase()).ConfigureAwait(false);
        }
    }
}
