using System;
using System.Collections.Generic;

namespace PersonListener.Domain.Account
{
    public class AccountResponseObject
    {
        public Guid Id { get; set; }
        public string PaymentReference { get; set; }
        public Guid ParentAccount { get; set; }
        public string TargetType { get; set; }
        public string TargetId { get; set; }
        public string AccountType { get; set; }
        public string RentGroupType { get; set; }
        public string AgreementType { get; set; }
        public Decimal AccountBalance { get; set; }
        public string LastUpdated { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string AccountStatus { get; set; }
        public List<ConsolidatedCharge> ConsolidatedCharges { get; set; }
        public AccountTenure Tenure { get; set; }
    }
}
