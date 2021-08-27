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
        HouseholdMember
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Gender
    {
        M,  // Male
        F,  // Female
        O   // Other?
    }
}
