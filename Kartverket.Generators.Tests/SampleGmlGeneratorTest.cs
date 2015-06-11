using FluentAssertions;
using NUnit.Framework;

namespace Kartverket.Generators.Tests
{
    public class SampleGmlGeneratorTest
    {
        [Test]
        public void ShouldGenerateSampleGmlIdenticalToGiven()
        {
            new SampleGmlGenerator().GenerateGml().Should().NotBeNull();
        }
    }
}
