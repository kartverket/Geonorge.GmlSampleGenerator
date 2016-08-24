using FluentAssertions;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using static Kartverket.Generators.SampleGmlGenerator;

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
            string gmlFilePath = @"C:\temp\test.gml";
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

        [Test]
        public void ShouldGenerateXlinkElementForAttributesThatAreAbstractFeatures()
        {
            // Simulate stream from web project controller by opening a local file:
            string xsdFilename = "luftfartshindre.xsd";
            XNamespace appNamespace = "http://skjema.geonorge.no/SOSI/produktspesifikasjon/Luftfartshindre/20160202";
            
            using (FileStream xsdFileStream = File.Open(xsdFilename, FileMode.Open))
            {
                // Generate gml-stream from xsd-stream:
                using (MemoryStream gmlMemoryStream = new SampleGmlGenerator(xsdFileStream, xsdFilename).GenerateGml())
                {
                    SaveStreamToFile(gmlMemoryStream, "luftfartshindre.xml");

                    gmlMemoryStream.Seek(0, SeekOrigin.Begin); // must reset stream position after it has been read

                    XDocument gmlDoc = XDocument.Load(gmlMemoryStream);
                    gmlDoc.Should().NotBeNull();

                    XElement vertikalObjekt = gmlDoc.Element(XmlNsGml + "FeatureCollection")
                        .Element(XmlNsGml + "featureMembers")
                        .Element(appNamespace + "VertikalObjekt");

                    vertikalObjekt.Should().NotBeNull();

                    vertikalObjekt.Element(appNamespace + "bestårAvVertikalobjektkomponent").Attribute(XmlNsXlink + "href").Should().NotBeNull();

                    // endringsflagg skal være tilstede med alle sine egenskaper
                    vertikalObjekt.Element(appNamespace + "endringsflagg").HasElements.Should().BeTrue("endringsflagg is an abstract object and should be inlined");
                }
            }

            
            
        }

        private void SaveStreamToFile(MemoryStream inputStream, string filename) {
            string gmlFilePath = $@"C:\temp\{filename}";
            string targetDirectory = Path.GetDirectoryName(gmlFilePath);
            if (targetDirectory != null && !Directory.Exists(targetDirectory))
                Directory.CreateDirectory(targetDirectory);
            FileStream fileStream = new FileStream(gmlFilePath, FileMode.Create, FileAccess.Write);
            inputStream.WriteTo(fileStream);
        }

    }
}
