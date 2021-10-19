using System.Collections.Generic;

namespace PersonListener.Domain.Account
{
    public class AccountTenure
    {
        public string TenureId { get; set; }
        public AccountTenureType TenureType { get; set; }
        public List<AccountTenant> PrimaryTenants { get; set; }
        public string FullAddress { get; set; }
    }
}
