using System;
using System.Collections.Generic;

namespace PersonListener.Domain.Account
{
    public class AccountTenure
    {
        public Guid TenancyId { get; set; }
        public string TenancyType { get; set; }
        public List<AccountTenant> PrimaryTenants { get; set; }
        public string FullAddress { get; set; }
    }
}
