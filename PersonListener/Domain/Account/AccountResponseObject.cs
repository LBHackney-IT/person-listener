using System;
using System.Collections.Generic;

namespace PersonListener.Domain.Account
{
    public class AccountResponseObject
    {
        public Guid Id { get; set; }

        public decimal AccountBalance { get; set; }
        public decimal ConsolidatedBalance { get; set; }
        public List<ConsolidatedCharge> ConsolidatedCharges { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public Guid ParentAccountId { get; set; }
        public string PaymentReference { get; set; }
        public string TargetType { get; set; }
        public Guid TargetId { get; set; }
        public string AccountType { get; set; }
        public string RentGroupType { get; set; }
        public string AgreementType { get; set; }
        public string AccountStatus { get; set; }
        public AccountTenure Tenure { get; set; }

        public string CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string LastUpdatedAt { get; set; }
        public string LastUpdatedABy { get; set; }
    }
}
