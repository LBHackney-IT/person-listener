using System;
using System.Text;

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

        public override bool Equals(object obj)
        {
            if (GetType() != obj.GetType()) return false;
            var otherObj = (HouseholdMembers) obj;
            return Id.Equals(otherObj.Id)
                && Type.Equals(otherObj.Type)
                && FullName.Equals(otherObj.FullName)
                && IsResponsible.Equals(otherObj.IsResponsible)
                && DateOfBirth.Equals(otherObj.DateOfBirth)
                && PersonTenureType.Equals(otherObj.PersonTenureType);
        }

        public override int GetHashCode()
        {
            StringBuilder builder = new StringBuilder();
            return builder.Append(Id.ToString())
                          .Append(Type)
                          .Append(FullName)
                          .Append(IsResponsible)
                          .Append(DateOfBirth)
                          .Append(PersonTenureType)
                          .ToString()
                          .GetHashCode();
        }
    }
}
