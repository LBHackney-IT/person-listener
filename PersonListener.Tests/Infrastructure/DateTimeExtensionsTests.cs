using FluentAssertions;
using PersonListener.Infrastructure;
using System;
using Xunit;

namespace PersonListener.Tests.Infrastructure
{
    public class DateTimeExtensionsTests
    {
        [Fact]
        public void ToFormattedDateTimeTestReturnsFormattedString()
        {
            new DateTime(2021, 6, 6).ToFormattedDateTime().Should().Be("2021-06-06T00:00:00.0000000Z");
            DateTime.MinValue.ToFormattedDateTime().Should().Be("0001-01-01T00:00:00.0000000Z");
            DateTime.MaxValue.ToFormattedDateTime().Should().Be("9999-12-31T23:59:59.9999999Z");
        }
    }
}
