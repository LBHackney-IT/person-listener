using FluentAssertions;
using PersonListener.Infrastructure.Exceptions;
using System;
using Xunit;

namespace PersonListener.Tests.Infrastructure.Exceptions
{
    public class HouseholdMembersNotChangedExceptionTests
    {
        [Fact]
        public void HouseholdMembersNotChangedExceptionConstructorTest()
        {
            var id = Guid.NewGuid();
            var correlationId = Guid.NewGuid();

            var ex = new HouseholdMembersNotChangedException(id, correlationId);
            ex.TenureId.Should().Be(id);
            ex.CorrelationId.Should().Be(correlationId);

            ex.Message.Should().Be($"There are no new or changed household member records on the tenure (id: {id}) resulting from the {EventTypes.PersonAddedToTenureEvent} with correlation id {correlationId}");
        }
    }
}
