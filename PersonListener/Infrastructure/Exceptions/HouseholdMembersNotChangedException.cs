using System;

namespace PersonListener.Infrastructure.Exceptions
{
    public class HouseholdMembersNotChangedException : Exception
    {
        public Guid TenureId { get; }
        public Guid CorrelationId { get; }

        public HouseholdMembersNotChangedException(Guid tenureId, Guid correlationId)
            : base($"There are no new or changed household member records on the tenure (id: {tenureId}) resulting from the {EventTypes.PersonAddedToTenureEvent} with correlation id {correlationId}")
        {
            TenureId = tenureId;
            CorrelationId = correlationId;
        }
    }
}
