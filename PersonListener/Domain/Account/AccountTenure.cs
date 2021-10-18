using System.Collections.Generic;

namespace PersonListener.Domain.Account
{
    public class AccountTenure
    {
        public string TenureId { get; set; }
        public string TenureType { get; set; }
        public List<AccountTenureType> PrimaryTenants { get; set; }
        public string FullAddress { get; set; }
    }
}
