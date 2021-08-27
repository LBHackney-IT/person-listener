using FluentAssertions;
using PersonListener.Infrastructure.Exceptions;
using System;
using System.Net;
using Xunit;

namespace PersonListener.Tests.Infrastructure.Exceptions
{
    public class GetTenureExceptionTests
    {
        [Fact]
        public void GetTenureExceptionConstructorTest()
        {
            var personId = Guid.NewGuid();
            var statusCode = HttpStatusCode.OK;
            var msg = "Some API error message";

            var ex = new GetTenureException(personId, statusCode, msg);
            ex.PersonId.Should().Be(personId);
            ex.StatusCode.Should().Be(statusCode);
            ex.ResponseBody.Should().Be(msg);
            ex.Message.Should().Be($"Failed to get tenure details for id {personId}. Status code: {statusCode}; Message: {msg}");
        }
    }
}
