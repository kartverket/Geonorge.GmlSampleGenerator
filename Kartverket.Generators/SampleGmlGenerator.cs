using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public static readonly XNamespace XmlNsXsd = "http://www.w3.org/2001/XMLSchema";
        public static readonly XNamespace XmlNsGml = "http://www.opengis.net/gml/3.2";
        public static readonly XNamespace XmlNsXlink = "http://www.w3.org/1999/xlink";
        private static readonly XNamespace XmlNsXsi = "http://www.w3.org/2001/XMLSchema-instance";
        private XNamespace _targetNamespace;

        private SampleGmlDataGenerator _sampleDataGenerator;

        private Dictionary<XName, int> _frequencyRestrictedTypes;
        private const int MaxInstancesRestrictedTypes = 5;

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
            _sampleDataGenerator = new SampleGmlDataGenerator(gmlSettings, _targetNamespace, XmlNsGml);
            _frequencyRestrictedTypes = new Dictionary<XName, int>();
        }

        public MemoryStream GenerateGml()
        {
            XDocument gmlDoc = new XDocument();

            XAttribute gmlIdAttribute = new XAttribute(XmlNsGml + "id", "_" + Guid.NewGuid().ToString());

            XElement featureCollection = new XElement(XmlNsGml + "FeatureCollection", gmlIdAttribute, GenerateNamespaces());

            XElement featureMembers = new XElement(XmlNsGml + "featureMembers");

            GenerateFeatureData(featureMembers, GetBaseClasses());

            foreach (var featureMember in featureMembers.Elements())
                featureMember.Add(new XAttribute(XmlNsGml + "id", "_" + Guid.NewGuid()));

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

            IEnumerable<XElement> elementsFromContainer = GetElementsFromContainer(xsdPropertyContainer);
            if (elementsFromContainer != null)
            {
                foreach (XElement xsdPropertyElm in elementsFromContainer)
                {
                    GeneratePropertyData(gmlDataContainer, xsdPropertyElm);
                }
            }
        }

        private IEnumerable<XElement> GetElementsFromContainer(XElement container)
        {
            IEnumerable<XElement> elements = container.Element(XmlNsXsd + "complexContent")?
                .Element(XmlNsXsd + "extension")?.Element(XmlNsXsd + "sequence")?.Elements(XmlNsXsd + "element");

            if (elements == null)
            {
                elements = container.Element(XmlNsXsd + "sequence")?.Elements(XmlNsXsd + "element");
            }

            if (elements == null)
            {
                elements = container?.Elements(XmlNsXsd + "element");
            }
            return elements;

        }

        private void GeneratePropertyData(XElement gmlDataContainer, XElement xsdPropertyElm)
        {
            XName featureMembers = XName.Get("featureMembers", XmlNsGml.NamespaceName);

            if (xsdPropertyElm == null) return;
            if (IsAssignable(xsdPropertyElm)) {

                XElement gmlDataElm = new XElement(_targetNamespace + xsdPropertyElm.Attribute("name").Value); 
                gmlDataContainer.Add(gmlDataElm); 
                AssignSampleValue(gmlDataElm, xsdPropertyElm);
            }
            else if (!(gmlDataContainer.Name == featureMembers) && ParentIsAbstractFeatureType(xsdPropertyElm))
            {
                Trace.WriteLine(xsdPropertyElm.Attribute("name").Value + " is an abstract feature type.");

                XElement gmlDataElm = new XElement(_targetNamespace + xsdPropertyElm.Attribute("name").Value, new XAttribute(XmlNsXlink + "href", "todo_realid_" + Guid.NewGuid().ToString()));
                gmlDataContainer.Add(gmlDataElm);
            }
            else if (IsGroupelement(xsdPropertyElm)) 
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

        private XElement LookupParentFromTypeAttribute(XElement element)
        {
            XElement parent = null;
            XAttribute typeAttribute = element.Attribute("type");
            if (typeAttribute != null)
            {
                parent = GetElementByName(typeAttribute.Value.Replace("app:", ""));
            }
            return parent;
        }

        private XElement LookupParentFromExtensionReference(XElement element)
        {
            XElement parent = null;
            XAttribute referedClass = element.Element(XmlNsXsd + "complexType")
               ?.Element(XmlNsXsd + "complexContent")
               ?.Element(XmlNsXsd + "extension")
               ?.Element(XmlNsXsd + "sequence")
               ?.Element(XmlNsXsd + "element")
               ?.Attribute("ref");
            if (referedClass != null)
            {
                parent = GetElementByName(referedClass.Value.Replace("app:", ""));
            }
            return parent;
        }

        private bool ParentIsAbstractFeatureType(XElement xsdPropertyElm)
        {
            XElement parent = LookupParentFromSubstitutionGroup(xsdPropertyElm);
            if (parent != null)
            {
                if (HasAbstractFeatureSubstitutionGroup(parent))
                {
                    return true;
                }
                else
                {
                    return ParentIsAbstractFeatureType(parent);
                }
            }

            parent = LookupParentFromTypeAttribute(xsdPropertyElm);
            if (parent != null)
                return ParentIsAbstractFeatureType(parent);

            parent = LookupParentFromExtensionReference(xsdPropertyElm);
            if (parent != null)
                return ParentIsAbstractFeatureType(parent);

            return false;
        }

        private bool HasAbstractFeatureSubstitutionGroup(XElement element)
        {
            bool isAbstractFeature = false;
            XAttribute substitutionGroup = element.Attribute("substitutionGroup");
            if (substitutionGroup != null)
            {
                Trace.WriteLine("element: " + element.Attribute("name").Value + ", substitutionGroup: " + substitutionGroup.Value);

                isAbstractFeature = substitutionGroup.Value.Equals("gml:AbstractFeature");
            }
            return isAbstractFeature;
        }


        private XElement LookupParentFromSubstitutionGroup(XElement element)
        {
            XAttribute substitutionGroup = element.Attribute("substitutionGroup");
            if (substitutionGroup != null)
            {
                Trace.WriteLine("element: " + element.Attribute("name").Value + ", substitutionGroup: " + substitutionGroup.Value);

                if (substitutionGroup.Value.Equals("gml:AbstractFeature"))
                    return element;
                else
                {
                    return GetElementByName(substitutionGroup.Value.Replace("app:", ""));
                }
            }
            return null;
        }

        private bool IsAbstractFeatureType(XElement xsdPropertyElm)
        {
            XAttribute substitutionGroup = xsdPropertyElm.Attribute("substitutionGroup");
            if (substitutionGroup != null)
            {
                Trace.WriteLine("element: " + xsdPropertyElm.Attribute("name").Value + ", substitutionGroup: " + substitutionGroup.Value);

                if (substitutionGroup.Value.Equals("gml:AbstractFeature"))
                    return true;
                else
                {
                    XElement parentElement = GetElementByName(substitutionGroup.Value.Replace("app:", ""));
                    if (parentElement != null)
                        return IsAbstractFeatureType(parentElement);
                }
            }

           
            return false;
        }



        private bool HasAssociationAttributeGroup(XElement xsdPropertyElm) {
            bool hasAssociationAttributeGroup = false;

            if (xsdPropertyElm.Attribute("type") == null)
            {
                foreach (XElement elm in xsdPropertyElm.Descendants(GetXName("attributeGroup")))
                {
                    XAttribute refAttr = elm.Attribute("ref");
                    if (refAttr != null && refAttr.Value == "gml:AssociationAttributeGroup")
                        hasAssociationAttributeGroup = true;
                }
            }
            else {
                XElement elm = GetElementByAttribute(xsdPropertyElm.Attribute("type"));
                foreach (XElement elm2 in xsdPropertyElm.Descendants(GetXName("attributeGroup")))
                {
                    XAttribute refAttr = elm2.Attribute("ref");
                    if (refAttr != null && refAttr.Value == "gml:AssociationAttributeGroup")
                        hasAssociationAttributeGroup = true;
                }
            }
            

            return hasAssociationAttributeGroup;
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
            return _frequencyRestrictedTypes[typeName] > MaxInstancesRestrictedTypes;
        }

        private bool IsExtension(XElement xsdPropertyContainer)
        {
            XElement complexContentElement = xsdPropertyContainer.Element(GetXName("complexContent"));
            XElement extensionElement = complexContentElement != null ? complexContentElement.Element(GetXName("extension")) : null;
            return extensionElement != null && HasAttributeDefined("base", extensionElement);
        }

        private XElement GetBaseClasses()
        {
            IEnumerable<XElement> baseClasses = (from classElm in _xsdDoc.Element(GetXName("schema")).Elements()
                                where IsRealizable(classElm)
                                    && !(HasAttributeDefined("substitutionGroup", classElm) && classElm.Attribute("substitutionGroup").Value.Equals("gml:AbstractObject"))
                                select classElm);

            return new XElement(XmlNsXsd + "schema", baseClasses);
        }
        
        // TODO: Improvement - Create [ bool HasAttributeDefinasAs(XElement element, string attrName, string attrValue) ]

        private bool IsGroupelement(XElement xsdElement)
        {
            /*
            bool isref = false;
            if (HasAttributeDefined("ref", xsdElement) && xsdElement.Parent != null && !HasAssociationAttributeGroup(xsdElement)) {
                isref = true;
                if (xsdElement.Parent.Parent != null) isref = false;
            }
            return isref;
            */
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
            if (typeAttr != null)
                return _sampleDataGenerator.SupportsType(typeAttr.Value) || IsEnumType(typeAttr) || IsCodeType(typeAttr);
            else
                return false;
        }



        private void AssignSampleValue(XElement gmlElement, XElement xsdPropertyElm)
        {
            object sampledata;

            XAttribute typeAttr = xsdPropertyElm.Attribute("type");

            if (IsEnumType(typeAttr))
            {
                sampledata = PickEnumValue(typeAttr);
            }
            else if(IsCodeType(typeAttr))
            {
                sampledata = "-kodelisteverdi-"; // TODO: Pick from code-list
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

        private bool IsCodeType(XAttribute typeAttr)
        {
            return typeAttr.Value.Equals("gml:CodeType");
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
            return XName.Get(elementName, XmlNsXsd.NamespaceName);
        }

        private object[] GenerateNamespaces()
        {
            if (_targetNamespace == null || String.IsNullOrEmpty(_targetNamespace.NamespaceName))
                _targetNamespace = "http://www.ikkeangitt.no";

            return new object[] 
                {
                    new XAttribute(XNamespace.Xmlns + "gml", XmlNsGml),
                    new XAttribute(XNamespace.Xmlns + "app", _targetNamespace),
                    new XAttribute(XNamespace.Xmlns + "xlink", XmlNsXlink),
                    new XAttribute(XNamespace.Xmlns + "xsi", XmlNsXsi),
                    new XAttribute(XmlNsXsi + "schemaLocation", "" + _targetNamespace + " " + _targetNamespace + "/" + _xsdFilename)
                };
        }
    }
}
