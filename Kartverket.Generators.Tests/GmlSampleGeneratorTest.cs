using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using NUnit.Framework;

namespace Kartverket.Generators.Tests
{
    public class GmlSampleGeneratorTest
    {
        [Test]
        public void ShouldGenerateGmlSampleIdenticalToGiven()
        {
            // Test the test-framework/-method
            string text = "text";
            text.Should().Be("text");
        }
    }
}
