using System;

namespace PersonListener.Domain.TenureInformation
{
    public class TenuredAsset
    {
        public Guid Id { get; set; }

        public TenuredAssetType? Type { get; set; }

        public string FullAddress { get; set; }

        public string Uprn { get; set; }
    }
}
