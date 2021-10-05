namespace PersonListener
{
    public static class EventTypes
    {
        // Define the event types this service will be interested in here.
        public const string TenureCreatedEvent = "TenureCreatedEvent";
        public const string TenureUpdatedEvent = "TenureUpdatedEvent";
        public const string PersonAddedToTenureEvent = "PersonAddedToTenureEvent";
        public const string PersonRemovedFromTenureEvent = "PersonRemovedFromTenureEvent";
    }
}
