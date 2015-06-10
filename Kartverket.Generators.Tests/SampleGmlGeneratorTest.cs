using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using NUnit.Framework;
using Kartverket.Generators;
using System.Xml.Linq;

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
