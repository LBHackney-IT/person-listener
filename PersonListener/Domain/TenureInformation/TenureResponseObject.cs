using System;
using System.Collections.Generic;

namespace PersonListener.Domain.TenureInformation
{
    public class TenureResponseObject
    {
        public Guid Id { get; set; }
        public string PaymentReference { get; set; }
        public TenuredAsset TenuredAsset { get; set; }
        public DateTime StartOfTenureDate { get; set; }
        public DateTime? EndOfTenureDate { get; set; }
        public TenureType TenureType { get; set; }
        public List<HouseholdMembers> HouseholdMembers { get; set; }
    }
}
