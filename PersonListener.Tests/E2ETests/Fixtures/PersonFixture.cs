using Amazon.DynamoDBv2.DataModel;
using AutoFixture;
using Hackney.Shared.Person;
using Hackney.Shared.Person.Domain;
using Hackney.Shared.Person.Factories;
using Hackney.Shared.Person.Infrastructure;
using Hackney.Shared.Tenure.Boundary.Response;
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

        public List<PersonDbEntity> PersonsDbEntity { get; private set; } = new List<PersonDbEntity>();

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
                var personTenures = _fixture.CreateMany<TenureDetails>(2).ToList();
                personTenures.Add(_fixture.Build<TenureDetails>()
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

                PersonsDbEntity.Add(dbPerson);
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

        public Person GivenThePersonExistsWithTenure(Guid id, Guid tenureId)
        {
            var personTypes = new List<PersonType> { PersonType.Tenant, PersonType.HouseholdMember, PersonType.Freeholder };
            var ResponseObject = _fixture.Build<Person>()
                                     .With(x => x.Id, id)
                                     .With(x => x.DateOfBirth, DateTime.UtcNow.AddYears(-30))
                                     .With(x => x.PersonTypes, personTypes)
                                     .With(x => x.Tenures, _fixture.CreateMany<TenureDetails>(3).ToList())
                                     .With(x => x.VersionNumber, (int?) null)
                                     .Create();

            ResponseObject.Tenures.Last().Id = tenureId;
            var dbEntity = ResponseObject.ToDatabase();
            _dbContext.SaveAsync(dbEntity).GetAwaiter().GetResult();
            ResponseObject.VersionNumber = 0;
            DbEntity = dbEntity;
            return ResponseObject;
        }
    }
}
