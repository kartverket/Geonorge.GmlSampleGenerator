using FluentAssertions;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace Kartverket.Generators.Tests
{
    public class SampleGmlGeneratorTest
    {
        [Test]
        public void ShouldGenerateSampleGmlIdenticalToGiven()
        {
            // Simulate stream from web project controller by opening a local file:
            string xsdFilename = "lufthavn_el_2_0.xsd";
            FileStream xsdFileStream = File.Open(xsdFilename, FileMode.Open);

            // Generate gml-stream from xsd-stream:
            MemoryStream gmlMemoryStream = new SampleGmlGenerator(xsdFileStream, xsdFilename).GenerateGml();

            // Close the xsd-stream:
            xsdFileStream.Close();

            // Save the gml on disk for the purpose of this test:
            string gmlFilePath = @"C:\Users\Jørgen\Dropbox (Arkitektum AS)\dev\Kartverket.GmlSampleGenerator\Kartverket.GmlSampleGenerator\generated\test.gml";
            string targetDirectory = Path.GetDirectoryName(gmlFilePath);
            if (!Directory.Exists(targetDirectory))
                Directory.CreateDirectory(targetDirectory);
            FileStream gmlFileStream = new FileStream(gmlFilePath, FileMode.Create, FileAccess.Write);
            gmlMemoryStream.WriteTo(gmlFileStream);

            // Close the gml-streams:
            gmlMemoryStream.Close();
            gmlFileStream.Close();

            // Load the new gml-file as an XDocument:
            XDocument gmlDoc = XDocument.Load(gmlFilePath);

            // Tmp. test:
            gmlDoc.Should().NotBeNull();

            // Final test: gmlDoc somehow equals referenceGml...
            //XDocument referenceGml = XDocument.Load("Lufthavn_El.gml");
        }
    }
}
