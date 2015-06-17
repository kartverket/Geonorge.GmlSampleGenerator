using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;

namespace Kartverket.Generators
{
    public class SampleGmlGenerator
    {

        private static XNamespace XSD_NAMESPACE = "http://www.w3.org/2001/XMLSchema"; // TODO: Get from xsdDoc? '.GetDefaultNamespace()'
        private static XNamespace GML_NAMESPACE = "http://www.opengis.net/gml/3.2"; // TODO: Get from xsdDoc?
        private static XNamespace XLINK_NAMESPACE = "http://www.w3.org/1999/xlink"; // TODO: Get from xsdDoc?
        private static XNamespace XSI_NAMESPACE = "http://www.w3.org/2001/XMLSchema-instance"; // TODO: Get from xsdDoc?
        private XNamespace _appNamespace; // TODO: Make lokal? Rename to targetNamespace?

        private XDocument _xsdDoc;
        private string _xsdFilename;
        private GmlSettings _gmlSettings;

        public SampleGmlGenerator(string xsdFilename)
        {
            Initialize(xsdFilename, GetDefaultGmlSettings());
        }

        public SampleGmlGenerator(string xsdFilename, GmlSettings gmlSettings)
        {
            Initialize(xsdFilename, gmlSettings);
        }

        private void Initialize(string xsdFilename, GmlSettings gmlSettings)
        {
            _xsdFilename = xsdFilename;
            _xsdDoc = XDocument.Load(xsdFilename);
            _gmlSettings = gmlSettings;
        }

        public XDocument GenerateGml()
        {
            XDocument gmlDoc = new XDocument();

            XAttribute gmlIdAttribute = new XAttribute(GML_NAMESPACE + "id", "_" + Guid.NewGuid().ToString());

            string xmlns = "app"; // TODO: Get from xsdDoc? (= gottenFromXsdDoc ? "app" : "feil")
            string targetNamespace = "http://skjema.geonorge.no/SOSI/produktspesifikasjon/avinor/lufthavn_el/2.0"; // TODO: Get from xsdDoc: '<schema targetNamespace="...';
            string xsdDocument = _xsdFilename;

            Object[] namespaces = SetupNamespaces(xmlns, targetNamespace, xsdDocument);

            XElement featureCollection = new XElement(GML_NAMESPACE + "FeatureCollection", gmlIdAttribute, namespaces);

            XElement featureMembers = new XElement(GML_NAMESPACE + "featureMembers", GenerateFeaturemembers());

            featureCollection.Add(featureMembers);

            gmlDoc.Add(featureCollection);


            return gmlDoc;
        }

        private List<XElement> GenerateFeaturemembers()
        {

            List<XElement> featuremembers = new List<XElement>();

            List<XElement> instantiableClasses = GetInstantiableClasses();

            foreach (XElement instantiableClass in instantiableClasses)
            {
                XElement featuremember = GenerateFeaturemember(instantiableClass);
                if (featuremember != null) featuremembers.Add(featuremember);
            }

            return featuremembers;
        }

        private XElement GenerateFeaturemember(XElement instantiableClass)
        {
            if (instantiableClass.Attribute("name") != null)
                return new XElement(instantiableClass.Attribute("name").Value);

            return null;
        }

        private List<XElement> GetInstantiableClasses()
        {
            return (from cls in _xsdDoc.Element(GetXName("schema")).Elements(GetXName("element")) where !IsAbstract(cls) select cls).ToList();
        }


        private bool IsAbstract(XElement cls)
        {
            return (cls.Attribute("abstract") != null && cls.Attribute("abstract").Value.Equals("true")) ||
                   (cls.Attribute("substitutionGroup") != null && cls.Attribute("substitutionGroup").Value.Equals("gml:AbstractObject"));
        }


        private XName GetXName(string elementName)
        {
            return XName.Get(elementName, XSD_NAMESPACE.NamespaceName);
        }


        private object[] SetupNamespaces(string xmlns, string targetNamespace, string xsdDocument)
        {
            // TODO: Set in contructor?
            _appNamespace = targetNamespace;

            if (_appNamespace == null || String.IsNullOrEmpty(_appNamespace.NamespaceName))
            {
                _appNamespace = "http://www.ikkeangitt.no";
                targetNamespace = "http://www.ikkeangitt.no";
            }

            if (String.IsNullOrEmpty(xsdDocument))
            {
                xsdDocument = "feil.xsd";
            }

            object[] namespaces = new object[] 
                {
                    new XAttribute(XNamespace.Xmlns + "gml", GML_NAMESPACE),
                    new XAttribute(XNamespace.Xmlns + xmlns, _appNamespace),
                    new XAttribute(XNamespace.Xmlns + "xlink", XLINK_NAMESPACE),
                    new XAttribute(XNamespace.Xmlns + "xsi", XSI_NAMESPACE),
                    new XAttribute(XSI_NAMESPACE + "schemaLocation", "" + targetNamespace + " " + targetNamespace + "/" + xsdDocument)
                };

            return namespaces;
        }

        private GmlSettings GetDefaultGmlSettings()
        {
            return new GmlSettings()
                  {
                      use2DGeometry = true,
                      useRandomCoords = true,
                      skipSchemalocation = true,
                      useSharedGeometry = true
                  };
        }
    }
}
