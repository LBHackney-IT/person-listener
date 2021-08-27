using System;

namespace PersonListener.Domain.TenureInformation
{
    public class HouseholdMembers
    {
        public Guid Id { get; set; }
        public string Type { get; set; }
        public string FullName { get; set; }
        public bool IsResponsible { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string PersonTenureType { get; set; }
    }
}
