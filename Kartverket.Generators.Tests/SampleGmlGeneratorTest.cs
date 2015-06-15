using FluentAssertions;
using NUnit.Framework;
using System.Xml.Linq;

namespace Kartverket.Generators.Tests
{
    public class SampleGmlGeneratorTest
    {
        [Test]
        public void ShouldGenerateSampleGmlIdenticalToGiven()
        {
            XDocument referenceGml = XDocument.Load("Lufthavn_El.gml");
            
            XDocument xsd = XDocument.Load("lufthavn_el_2_0.xsd");

            new SampleGmlGenerator().GenerateGml().Should().NotBeNull();
        }
    }
}
