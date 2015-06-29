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
            SampleDataGenerator dataGenerator = new SampleDataGenerator();
            dataGenerator.SupportsType("string").Should().BeTrue();
            dataGenerator.SupportsType("noType").Should().BeFalse();
        }
        
        [Test]
        public void ShouldGenerateDataAccordingToGivenTypes()
        {
            SampleDataGenerator dataGenerator = new SampleDataGenerator();

            string stringData = (string)dataGenerator.GenerateForType("string");
            stringData.GetType().Should().Be(typeof(string));

            int intData = (int)dataGenerator.GenerateForType("integer");
            intData.GetType().Should().Be(typeof(int));

            double doubleData = (double)dataGenerator.GenerateForType("double");
            doubleData.GetType().Should().Be(typeof(double));

            DateTime dateTimeData = (DateTime)dataGenerator.GenerateForType("dateTime");
            dateTimeData.GetType().Should().Be(typeof(DateTime));
        }
    }
}
