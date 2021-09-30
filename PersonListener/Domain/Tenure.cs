using System;
using System.Text;
using System.Text.Json.Serialization;

namespace PersonListener.Domain
{
    public class Tenure
    {
        public string AssetFullAddress { get; set; }

        public string AssetId { get; set; }

        public string StartDate { get; set; }

        public string EndDate { get; set; }

        public Guid Id { get; set; }

        public string Type { get; set; }

        public string Uprn { get; set; }

        public string PaymentReference { get; set; }

        public string PropertyReference { get; set; }

        [JsonIgnore]
        public bool IsActive => TenureHelpers.IsTenureActive(EndDate);

        public override bool Equals(object obj)
        {
            if (GetType() != obj?.GetType()) return false;
            var otherObj = (Tenure) obj;
            return otherObj != null
                && (String.Compare(AssetFullAddress, otherObj.AssetFullAddress) == 0)
                && (String.Compare(AssetId, otherObj.AssetId) == 0)
                && (String.Compare(StartDate, otherObj.StartDate) == 0)
                && (String.Compare(EndDate, otherObj.EndDate) == 0)
                && Id.Equals(otherObj.Id)
                && (String.Compare(Type, otherObj.Type) == 0)
                && (String.Compare(Uprn, otherObj.Uprn) == 0)
                && (String.Compare(PaymentReference, otherObj.PaymentReference) == 0)
                && (String.Compare(PropertyReference, otherObj.PropertyReference) == 0);
        }

        public override int GetHashCode()
        {
            StringBuilder builder = new StringBuilder();
            return builder.Append(AssetFullAddress)
                          .Append(AssetId)
                          .Append(StartDate)
                          .Append(EndDate)
                          .Append(Id.ToString())
                          .Append(Type)
                          .Append(Uprn)
                          .Append(PaymentReference)
                          .Append(PropertyReference)
                          .Append(IsActive)
                          .ToString()
                          .GetHashCode();
        }
    }
}
