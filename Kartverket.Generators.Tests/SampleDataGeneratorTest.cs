using FluentAssertions;
using NUnit.Framework;
using System;
using System.Xml.Linq;

namespace Kartverket.Generators.Tests
{
    public class SampleDataGeneratorTest
    {
        [Test]
        public void ShouldDeterminWhetherTypeIsSupported()
        {
            SampleDataGenerator.SupportsType("string").Should().BeTrue();
            SampleDataGenerator.SupportsType("noType").Should().BeFalse();
        }
        
        [Test]
        public void ShouldGenerateDataAccordingToGivenTypes()
        {
            string stringData = (string) SampleDataGenerator.GenerateForType("string");
            stringData.GetType().Should().Be(typeof(string));

            int intData = (int) SampleDataGenerator.GenerateForType("integer");
            intData.GetType().Should().Be(typeof(int));

            double doubleData = (double) SampleDataGenerator.GenerateForType("double");
            doubleData.GetType().Should().Be(typeof(double));

            DateTime dateTimeData = (DateTime) SampleDataGenerator.GenerateForType("dateTime");
            dateTimeData.GetType().Should().Be(typeof(DateTime));
        }
    }
}
