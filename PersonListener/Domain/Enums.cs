using System.Text.Json.Serialization;

namespace PersonListener.Domain
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Title
    {
        Dr,
        Master,
        Miss,
        Mr,
        Mrs,
        Ms,
        Other,
        Rabbi,
        Reverend
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PersonType
    {
        Tenant,
        HouseholdMember,
        Leaseholder,
        Freeholder,
        Occupant
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Gender
    {
        M,  // Male
        F,  // Female
        O   // Other?
    }
}
