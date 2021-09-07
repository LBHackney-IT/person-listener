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
            var id = Guid.NewGuid();
            var statusCode = HttpStatusCode.OK;
            var msg = "Some API error message";

            var ex = new GetTenureException(id, statusCode, msg);
            ex.TenureId.Should().Be(id);
            ex.StatusCode.Should().Be(statusCode);
            ex.ResponseBody.Should().Be(msg);
            ex.Message.Should().Be($"Failed to get tenure details for id {id}. Status code: {statusCode}; Message: {msg}");
        }
    }
}
