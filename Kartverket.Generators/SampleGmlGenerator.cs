using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using System.IO;

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

        private SampleGmlDataGenerator _sampleDataGenerator;

        private Dictionary<XName, int> _frequencyRestrictedTypes;
        private const int MAX_INSTANCES_RESTRICTED_TYPES = 5;


        public SampleGmlGenerator(Stream xsdStream, string xsdFilename)
        {
            GmlSettings defaultGmlSettings = new GmlSettings();
            Initialize(xsdStream, xsdFilename, defaultGmlSettings);
        }

        public SampleGmlGenerator(Stream xsdStream, string xsdFilename, GmlSettings gmlSettings)
        {
            Initialize(xsdStream, xsdFilename, gmlSettings);
        }

        private void Initialize(Stream xsdStream, string xsdFilename, GmlSettings gmlSettings)
        {
            _xsdDoc = XDocument.Load(xsdStream);
            _xsdFilename = xsdFilename;
            _targetNamespace = _xsdDoc.Element(GetXName("schema")).Attribute("targetNamespace").Value;
            _sampleDataGenerator = new SampleGmlDataGenerator(gmlSettings, _targetNamespace, _xmlns_gml);
            _frequencyRestrictedTypes = new Dictionary<XName, int>();
        }

        public MemoryStream GenerateGml()
        {
            XDocument gmlDoc = new XDocument();

            XAttribute gmlIdAttribute = new XAttribute(_xmlns_gml + "id", "_" + Guid.NewGuid().ToString());

            XElement featureCollection = new XElement(_xmlns_gml + "FeatureCollection", gmlIdAttribute, GenerateNamespaces());

            XElement featureMembers = new XElement(_xmlns_gml + "featureMembers");

            GenerateFeatureData(featureMembers, GetBaseClasses());

            featureCollection.Add(featureMembers);

            gmlDoc.Add(featureCollection);

            MemoryStream gmlStream = new MemoryStream();

            gmlDoc.Save(gmlStream);

            return gmlStream;
        }

        private void GenerateFeatureData(XElement gmlDataContainer, XElement xsdPropertyContainer)
        {
            if (xsdPropertyContainer == null) return;

            if (IsExtension(xsdPropertyContainer))
            {
                XAttribute baseAttr = xsdPropertyContainer
                                          .Element(GetXName("complexContent"))
                                              .Element(GetXName("extension"))
                                                  .Attribute("base");

                XElement baseContainer = GetElementByAttribute(baseAttr);
                GenerateFeatureData(gmlDataContainer, baseContainer);
            }

            foreach (XElement xsdPropertyElm in xsdPropertyContainer.Descendants(GetXName("element")))
            {
                GeneratePropertyData(gmlDataContainer, xsdPropertyElm);
            }
        }

        private void GeneratePropertyData(XElement gmlDataContainer, XElement xsdPropertyElm)
        {
            if (xsdPropertyElm == null) return;

            if (IsRefferer(xsdPropertyElm))
            {
                XAttribute refAttr = xsdPropertyElm.Attribute("ref");
                GeneratePropertyData(gmlDataContainer, GetElementByAttribute(refAttr));
            }
            else if (IsRealizable(xsdPropertyElm))
            {
                if (HasInstanceRestriction(xsdPropertyElm))
                {
                    TrackFrequency(xsdPropertyElm);

                    if (InstanceRestrictionIsExceeded(xsdPropertyElm)) return;
                }

                XElement gmlDataElm = new XElement(_targetNamespace + xsdPropertyElm.Attribute("name").Value); // TODO: Improvement - Create factory method for gmlElements?

                gmlDataContainer.Add(gmlDataElm); // TODO: Add multiple instances if property describes 0/1..many

                if (IsAssignable(xsdPropertyElm))
                    AssignSampleValue(gmlDataElm, xsdPropertyElm);
                else
                {
                    XAttribute typeAttr = xsdPropertyElm.Attribute("type");
                    GenerateFeatureData(gmlDataElm, GetElementByAttribute(typeAttr));
                }
            }
        }

        private bool HasInstanceRestriction(XElement xsdPropertyElm)
        {
            // All other than assignable types has instance restriction
            return !IsAssignable(xsdPropertyElm);
        }

        private void TrackFrequency(XElement xsdPropertyElm)
        {
            string typeName = xsdPropertyElm.Attribute("name").Value;

            if (_frequencyRestrictedTypes.ContainsKey(typeName))
                _frequencyRestrictedTypes[typeName]++;
            else
                _frequencyRestrictedTypes[typeName] = 1;
        }

        private bool InstanceRestrictionIsExceeded(XElement xsdPropertyElm)
        {
            string typeName = xsdPropertyElm.Attribute("name").Value;
            return _frequencyRestrictedTypes[typeName] > MAX_INSTANCES_RESTRICTED_TYPES;
        }

        private bool IsExtension(XElement xsdPropertyContainer)
        {
            XElement complexContentElement = xsdPropertyContainer.Element(GetXName("complexContent"));
            XElement extensionElement = complexContentElement != null ? complexContentElement.Element(GetXName("extension")) : null;
            return extensionElement != null && HasAttributeDefined("base", extensionElement);
        }

        private XElement GetBaseClasses()
        {
            object[] baseClasses = (from classElm in _xsdDoc.Element(GetXName("schema")).Elements()
                                where IsRealizable(classElm)
                                    && !(HasAttributeDefined("substitutionGroup", classElm) && classElm.Attribute("substitutionGroup").Value.Equals("gml:AbstractObject"))
                                select classElm).ToArray();

            return new XElement("BaseClasses", baseClasses);
        }
        
        // TODO: Improvement - Create [ bool HasAttributeDefinasAs(XElement element, string attrName, string attrValue) ]

        private bool IsRefferer(XElement xsdElement)
        {
            return HasAttributeDefined("ref", xsdElement);
        }

        private bool IsRealizable(XElement xsdElement)
        {
            return IsTag("element", xsdElement)
                   && HasAttributeDefined("name", xsdElement)
                   && HasAttributeDefined("type", xsdElement)
                   && !IsAbstract(xsdElement);
        }

        private bool IsAssignable(XElement xsdPropertyElm)
        {
            XAttribute typeAttr = xsdPropertyElm.Attribute("type");

            return _sampleDataGenerator.SupportsType(typeAttr.Value) || IsEnumType(typeAttr);
        }



        private void AssignSampleValue(XElement gmlElement, XElement xsdPropertyElm)
        {
            object sampledata;

            XAttribute typeAttr = xsdPropertyElm.Attribute("type");

            if (IsEnumType(typeAttr))
            {
                sampledata = PickEnumValue(typeAttr);
            }
            else
            {
                string type = xsdPropertyElm.Attribute("type").Value;
                sampledata = _sampleDataGenerator.GenerateForType(type);
            }

            gmlElement.Add(sampledata);
        }

        private string PickEnumValue(XAttribute typeAttr)
        {
            string enumValue = "enum-value";

            XElement simpleTypeElm = GetElementByAttribute(typeAttr);

            XElement restrictionElm = simpleTypeElm.Element(GetXName("restriction"));
            if (restrictionElm != null)
            {
                IEnumerable<XElement> enumElements = restrictionElm.Elements(GetXName("enumeration"));
                enumValue = PickRandomElement(enumElements).Attribute("value").Value;
            }
            else
            {
                XElement unionElm = simpleTypeElm.Element(GetXName("union"));
                if (unionElm != null) enumValue = PickEnumValue(unionElm.Attribute("memberTypes"));
            }

            return enumValue;
        }

        private XElement PickRandomElement(IEnumerable<XElement> enumElements)
        {
            if (enumElements == null || enumElements.Count() == 0) return null;

            return enumElements.ElementAt(new Random().Next(enumElements.Count()));
        }

        private bool IsEnumType(XAttribute typeAttr)
        {
            return IsTag("simpleType", GetElementByAttribute(typeAttr));
        }

        private bool IsTag(string tagName, XElement xsdElement)
        {
            return xsdElement != null && xsdElement.Name.LocalName == tagName;
        }

        private bool HasAttributeDefined(string attrName, XElement element)
        {
            return element.Attribute(attrName) != null && !string.IsNullOrEmpty(element.Attribute(attrName).Value);
        }

        private bool IsAbstract(XElement xsdClass)
        {
            return (xsdClass.Attribute("abstract") != null && xsdClass.Attribute("abstract").Value.Equals("true"));
        }

        private XElement GetElementByAttribute(XAttribute attribute)
        {
            return (attribute != null) ? GetElementByName(WithoutNSPrefix(SingleAttrValue(attribute.Value))) : null;
        }

        private string SingleAttrValue(string attrValue)
        {
            return attrValue.Split(' ')[0]; // "app:SomeValue app:AnotherValue" becomes "app:SomeValue"
        }

        private string WithoutNSPrefix(string prefixedValue)
        {
            return prefixedValue.Substring(prefixedValue.IndexOf(":") + 1);
        }

        private XElement GetElementByName(string elementName)
        {
            return (from element in _xsdDoc.Element(GetXName("schema")).Elements()
                    where element.Attribute("name") != null && element.Attribute("name").Value.Equals(elementName)
                    select element).FirstOrDefault();
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
    }
}
