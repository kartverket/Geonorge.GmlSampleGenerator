using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;

namespace Kartverket.Generators
{
    public class SampleGmlGenerator
    {
        private XDocument _xsdDoc;
        private string _xsdFilename;

        // TODO: Retrieve all namespaces from xsdDoc?
        private XNamespace _xmlns_xsd = "http://www.w3.org/2001/XMLSchema";
        private XNamespace _xmlns_gml = "http://www.opengis.net/gml/3.2";
        private XNamespace _xmlns_xlink = "http://www.w3.org/1999/xlink";
        private XNamespace _xmlns_xsi = "http://www.w3.org/2001/XMLSchema-instance";
        private XNamespace _targetNamespace;

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
            _targetNamespace = _xsdDoc.Element(GetXName("schema")).Attribute("targetNamespace").Value;
        }

        public XDocument GenerateGml()
        {
            XDocument gmlDoc = new XDocument();

            XAttribute gmlIdAttribute = new XAttribute(_xmlns_gml + "id", "_" + Guid.NewGuid().ToString());

            XElement featureCollection = new XElement(_xmlns_gml + "FeatureCollection", gmlIdAttribute, GenerateNamespaces());

            XElement featureMembers = new XElement(_xmlns_gml + "featureMembers", GenerateFeaturemembers());

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
            return XName.Get(elementName, _xmlns_xsd.NamespaceName);
        }


        private object[] GenerateNamespaces()
        {
            if (_targetNamespace == null || String.IsNullOrEmpty(_targetNamespace.NamespaceName))
                _targetNamespace = "http://www.ikkeangitt.no";

            return new object[] 
                {
                    new XAttribute(XNamespace.Xmlns + "gml", _xmlns_gml),
                    new XAttribute(XNamespace.Xmlns + "app", _targetNamespace),
                    new XAttribute(XNamespace.Xmlns + "xlink", _xmlns_xlink),
                    new XAttribute(XNamespace.Xmlns + "xsi", _xmlns_xsi),
                    new XAttribute(_xmlns_xsi + "schemaLocation", "" + _targetNamespace + " " + _targetNamespace + "/" + _xsdFilename)
                };
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
