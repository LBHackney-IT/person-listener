using PersonListener.Infrastructure.Exceptions;
using FluentAssertions;
using System;
using Xunit;

namespace PersonListener.Tests.Infrastructure.Exceptions
{
    public class PersonMissingTenureExceptionTests
    {
        [Fact]
        public void PersonMissingTenureExceptionConstructorTest()
        {
            var personId = Guid.NewGuid();
            var tenureId = Guid.NewGuid();

            var ex = new PersonMissingTenureException(personId, tenureId);
            ex.PersonId.Should().Be(personId);
            ex.TenureId.Should().Be(tenureId);
            ex.Message.Should().Be($"Person record with id {personId} does not have any tenure info for id {tenureId}");
        }
    }
}
