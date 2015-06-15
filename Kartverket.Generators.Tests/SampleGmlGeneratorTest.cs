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

            XDocument gmlDoc = new SampleGmlGenerator("lufthavn_el_2_0.xsd").GenerateGml();;

            // Tmp. test:
            gmlDoc.Should().NotBeNull();

            // Final test: gmlDoc equals referenceGml
        }
    }
}
