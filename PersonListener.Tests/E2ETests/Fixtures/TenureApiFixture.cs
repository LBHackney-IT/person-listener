using AutoFixture;
using Force.DeepCloner;
using Hackney.Core.Testing.Shared.E2E;
using Hackney.Shared.Person.Domain;
using Hackney.Shared.Person.Infrastructure;
using Hackney.Shared.Tenure.Boundary.Response;
using Hackney.Shared.Tenure.Domain;
using PersonListener.Boundary;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PersonListener.Tests.E2ETests.Fixtures
{
    public class TenureApiFixture : BaseApiFixture<TenureResponseObject>
    {
        private readonly Fixture _fixture = new Fixture();
        private const string TenureApiRoute = "http://localhost:5678/api/v1/";
        private const string TenureApiToken = "sdjkhfgsdkjfgsdjfgh";

        public Guid RemovedPersonId { get; private set; }
        public EventData MessageEventData { get; private set; }

        public TenureApiFixture()
            : base(TenureApiRoute, TenureApiToken)
        {
            Environment.SetEnvironmentVariable("TenureApiUrl", TenureApiRoute);
            Environment.SetEnvironmentVariable("TenureApiToken", TenureApiToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                base.Dispose(disposing);
            }
        }

        public void GivenThePersonTenuresExist(PersonDbEntity dbEntity)
        {
            for (int i = 0; i < dbEntity.Tenures.Count; i++)
            {
                var personTenure = dbEntity.Tenures[i];
                var personType = dbEntity.PersonTypes[i];
                var Tenure = CreateTenureForPerson(personTenure.Id, dbEntity.Id, personType);
                Responses.Add(Tenure.Id.ToString(), Tenure);
            }
        }

        private TenureResponseObject CreateTenureForPerson(Guid tenureId, Guid personId, PersonType personType)
        {
            TenureType tt;
            bool isResponsible;
            switch (personType)
            {
                case PersonType.HouseholdMember:
                    tt = TenureTypes.Secure;
                    isResponsible = false;
                    break;
                case PersonType.Freeholder:
                    tt = TenureTypes.Freehold;
                    isResponsible = true;
                    break;
                default:
                    tt = TenureTypes.Secure;
                    isResponsible = true;
                    break;
            }
            var hms = _fixture.Build<HouseholdMembers>()
                              .With(x => x.DateOfBirth, DateTime.UtcNow.AddYears(-40))
                              .With(x => x.PersonTenureType, (PersonTenureType) Enum.Parse(typeof(PersonTenureType), Enum.GetName(typeof(PersonType), personType)))
                              .With(x => x.IsResponsible, isResponsible)
                              .CreateMany(3).ToList();
            hms.Last().Id = personId;

            return _fixture.Build<TenureResponseObject>()
                           .With(x => x.Id, tenureId)
                           .With(x => x.TenureType, tt)
                           .With(x => x.HouseholdMembers, hms)
                           .Create();
        }

        public void GivenTheTenureHasNoHouseholdMembers(TenureResponseObject tenureResponse)
        {
            tenureResponse.HouseholdMembers.Clear();
        }

        public void GivenTheTenureDoesNotExist(Guid id)
        {
            // Nothing to do here
        }

        public TenureResponseObject GivenTheTenureExists(Guid id)
        {
            ResponseObject = _fixture.Build<TenureResponseObject>()
                                      .With(x => x.Id, id)
                                      .With(x => x.HouseholdMembers,
                                                 _fixture.Build<HouseholdMembers>()
                                                         .With(y => y.PersonTenureType, PersonTenureType.Occupant)
                                                         .CreateMany(3)
                                                         .ToList())
                                      .Create();
            return ResponseObject;
        }
        private List<HouseholdMembers> CreateHouseholdMembers(int count = 3)
        {
            return _fixture.Build<HouseholdMembers>()
                           .With(x => x.Id, () => Guid.NewGuid())
                           .With(x => x.DateOfBirth, DateTime.UtcNow.AddYears(-40))
                           .With(x => x.PersonTenureType, PersonTenureType.Tenant)
                           .CreateMany(count).ToList();
        }

        private void CreateMessageEventDataForPersonRemoved(Guid id)
        {
            var oldData = CreateHouseholdMembers();
            var newData = oldData.DeepClone();

            var removedHm = CreateHouseholdMembers(1).First();
            removedHm.Id = id;
            oldData.Add(removedHm);
            RemovedPersonId = id;

            MessageEventData = new EventData()
            {
                OldData = new Dictionary<string, object> { { "householdMembers", oldData } },
                NewData = new Dictionary<string, object> { { "householdMembers", newData } }
            };
        }

        public void GivenAPersonWasRemoved(Guid id)
        {
            CreateMessageEventDataForPersonRemoved(id);
        }
    }
}
