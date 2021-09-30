using AutoFixture;
using FluentAssertions;
using Force.DeepCloner;
using PersonListener.Domain.TenureInformation;
using System.Linq;
using Xunit;

namespace PersonListener.Tests.Domain.TenureInformation
{
    public class HouseholdMembersTests
    {
        private readonly Fixture _fixture = new Fixture();
        private HouseholdMembers _classUnderTest;

        [Fact]
        public void EqualsTestWrongTypeReturnsFalse()
        {
            _classUnderTest = _fixture.Create<HouseholdMembers>();
            _classUnderTest.Equals("some string").Should().BeFalse();
        }

        [Fact]
        public void EqualsTestSameObjectReturnsTrue()
        {
            _classUnderTest = _fixture.Create<HouseholdMembers>();
            _classUnderTest.Equals(_classUnderTest).Should().BeTrue();
        }

        [Fact]
        public void EqualsTestEquivalentObjectReturnsTrue()
        {
            _classUnderTest = _fixture.Create<HouseholdMembers>();
            var compare = _classUnderTest.DeepClone();
            _classUnderTest.Equals(compare).Should().BeTrue();
        }

        [Fact]
        public void EqualsTestDifferentObjectReturnsFalse()
        {
            _classUnderTest = _fixture.Create<HouseholdMembers>();
            var compare = _fixture.Create<HouseholdMembers>();
            _classUnderTest.Equals(compare).Should().BeFalse();
        }

        [Fact]
        public void EqualsTestNullObjectReturnsFalse()
        {
            _classUnderTest = _fixture.Create<HouseholdMembers>();
            _classUnderTest.Equals(null).Should().BeFalse();
        }

        [Fact]
        public void EqualsTestWithNullsEquivalentObjectReturnsTrue()
        {
            _classUnderTest = new HouseholdMembers();
            var compare = new HouseholdMembers();
            _classUnderTest.Equals(compare).Should().BeTrue();
        }

        [Fact]
        public void GetHashCodeTest()
        {
            _classUnderTest = _fixture.Create<HouseholdMembers>();
            var propValues = _classUnderTest.GetType()
                                .GetProperties()
                                .Select(x => x.GetValue(_classUnderTest).ToString());
            var expected = string.Join("", propValues).GetHashCode();
            _classUnderTest.GetHashCode().Should().Be(expected);
        }
    }
}
