using Amazon.DynamoDBv2.DataModel;
using AutoFixture;
using PersonListener.Domain;
using PersonListener.Domain.TenureInformation;
using PersonListener.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PersonListener.Tests.E2ETests.Fixtures
{
    public class PersonFixture : IDisposable
    {
        private readonly Fixture _fixture = new Fixture();

        private readonly IDynamoDBContext _dbContext;

        public PersonDbEntity DbEntity { get; private set; }
        public Guid DbEntityId { get; private set; }

        public List<PersonDbEntity> Persons { get; private set; } = new List<PersonDbEntity>();

        public PersonFixture(IDynamoDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool _disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                if (null != DbEntity)
                    _dbContext.DeleteAsync<PersonDbEntity>(DbEntity.Id).GetAwaiter().GetResult();

                _disposed = true;
            }
        }

        private PersonDbEntity ConstructAndSavePerson(Guid id)
        {
            var dbEntity = _fixture.Build<PersonDbEntity>()
                                 .With(x => x.Id, id)
                                 .With(x => x.DateOfBirth, DateTime.UtcNow.AddYears(-35))
                                 .With(x => x.LastModified, DateTime.UtcNow.AddHours(-1))
                                 .With(x => x.VersionNumber, (int?) null)
                                 .With(x => x.PersonTypes, new List<PersonType>(new[] { PersonType.Tenant }))
                                 .Create();

            _dbContext.SaveAsync<PersonDbEntity>(dbEntity).GetAwaiter().GetResult();
            dbEntity.VersionNumber = 0;
            return dbEntity;
        }

        public void GivenThePersonsAlreadyExist(TenureResponseObject tenure)
        {
            foreach (var hm in tenure.HouseholdMembers)
            {
                var personTenures = _fixture.CreateMany<Tenure>(2).ToList();
                personTenures.Add(_fixture.Build<Tenure>()
                                          .With(x => x.Id, tenure.Id)
                                          .Create());
                var dbPerson = _fixture.Build<PersonDbEntity>()
                                       .With(x => x.Id, hm.Id)
                                       .With(x => x.DateOfBirth, hm.DateOfBirth)
                                       .With(x => x.LastModified, DateTime.UtcNow.AddHours(-1))
                                       .With(x => x.Tenures, personTenures)
                                       .With(x => x.Id, hm.Id)
                                       .With(x => x.VersionNumber, (int?) null)
                                       .Create();

                _dbContext.SaveAsync<PersonDbEntity>(dbPerson).GetAwaiter().GetResult();
                dbPerson.VersionNumber = 0;

                Persons.Add(dbPerson);
            }
        }

        public void GivenAPersonAlreadyExists(Guid id)
        {
            if (null == DbEntity)
            {
                var entity = ConstructAndSavePerson(id);
                DbEntity = entity;
                DbEntityId = entity.Id;
            }
        }

        public void GivenAPersonDoesNotExist(Guid id)
        {
            // Nothing to do here
        }
    }
}
