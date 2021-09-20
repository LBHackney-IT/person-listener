using System;

namespace PersonListener.Infrastructure.Exceptions
{
    public class PersonMissingTenureException : Exception
    {
        public Guid PersonId { get; private set; }
        public Guid TenureId { get; private set; }

        public PersonMissingTenureException(Guid personId, Guid tenureId)
            : base($"Person record with id {personId} does not have any tenure info for id {tenureId}")
        {
            PersonId = personId;
            TenureId = tenureId;
        }
    }
}
