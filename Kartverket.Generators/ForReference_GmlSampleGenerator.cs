using System;
using System.IO;
using System.Xml.Linq;

namespace Kartverket.Generators
{
    public class GMLSettings
    {
        public bool use2DGeometry;
        public bool useRandomCoords;
        public bool skipSchemalocation;
        public bool useSharedGeometry;
    }

    public class GmlSampleGenerator
    {
        XNamespace _gml = "http://www.opengis.net/gml/3.2";
        XNamespace _xlink = "http://www.w3.org/1999/xlink";
        XNamespace _xsi = "http://www.w3.org/2001/XMLSchema-instance";
        XNamespace _app = "";
        private GMLSettings _settings;

        public GmlSampleGenerator(GMLSettings settings)
        {
            _settings = settings;
        }

        private object[] SetupNamespaces(string xmlns, string targetNamespace, string xsdDocument)
        {
            _app = targetNamespace;
            if (_app == null || String.IsNullOrEmpty(_app.NamespaceName))
            {
                _app = "http://www.ikkeangitt.no";
                targetNamespace = "http://www.ikkeangitt.no";
            }
            if (String.IsNullOrEmpty(xmlns))
            {
                xmlns = "feil";
            }
            if (String.IsNullOrEmpty(xsdDocument))
            {
                xsdDocument = "feil.xsd";
            }
            string schemaloc = "";
            if (_settings.skipSchemalocation == false) schemaloc = targetNamespace + " " + targetNamespace + "/" + xsdDocument;
            else schemaloc = targetNamespace + " " + xsdDocument;
            object[] namespaces = new object[] 
                {
                    new XAttribute(XNamespace.Xmlns + "gml", _gml),
                    new XAttribute(XNamespace.Xmlns + xmlns, _app),
                    new XAttribute(XNamespace.Xmlns + "xlink", _xlink),
                    new XAttribute(XNamespace.Xmlns + "xsi", _xsi),
                    new XAttribute(_xsi + "schemaLocation", "" + targetNamespace + " " + targetNamespace + "/" + xsdDocument)
                };
            return namespaces;
        }
        public void GenerateGMLSample(bool maxAttributes, string xsdSchema, string gmlfil)
        {
            try
            {
                //TODO 2D og 3D valg - om assosiasjoner skal være innline(skal komposisjoner alltid være innline?Nei kun datatyper) - om geometri skal forskyves(random) 
                //- hvor kompleks geometri skal brukes? - delt geometri? - hierarki i kodelister

                string xmlns = "app"; //GetTaggedValueFromElement(valgtPakke.Element, "xmlns");
                string targetNamespace = ""; //GetTaggedValueFromElement(valgtPakke.Element, "targetNamespace");
                string xsdDocument = "filnavn.xsd"; //GetTaggedValueFromElement(valgtPakke.Element, "xsdDocument");

              

                string katalog = Path.GetDirectoryName(gmlfil);

                if (!Directory.Exists(katalog))
                {
                    Directory.CreateDirectory(katalog);
                }

                //GML ID påføring
                XDocument doc = new XDocument();
                XElement fc = new XElement(_gml + "FeatureCollection", new XAttribute(_gml + "id", "_" + Guid.NewGuid().ToString()), SetupNamespaces(xmlns, targetNamespace, xsdDocument));


                XElement fm = new XElement(_gml + "featureMembers");

                fc.Add(fm);

                doc.Add(fc);
                ConvertClasses(valgtPakke, _repository, fm);

                doc.Save(gmlfil);

            }
            catch (Exception ex)
            {
               // Log("KRITISK FEIL: " + ex.Message);
            }
        }

        private void ConvertClasses(Package childPackage, Repository repository, XElement fm)
        {
            Package valgtPakke = childPackage;

            foreach (Element el in valgtPakke.Elements)
            {
                if (el.Type == "Class" && el.Abstract == "0" && el.Stereotype.ToLower() == "featuretype")
                {
                    LogDebug("Funne realiserbart objekt: " + el.Name);
                    ConvertClass(repository, fm, el);

                }

            }
            foreach (Package childPackage2 in valgtPakke.Packages)
            {
                ConvertClasses(childPackage2, repository, fm);
            }

        }

        private void ConvertClass(Repository repository, XElement fm, Element el)
        {
            XElement f = new XElement(_app + el.Name);
            string unikId = Guid.NewGuid().ToString();
            f.Add(new XAttribute(_gml + "id", "_" + unikId));

            fm.Add(f);

            ConvertAttributes(el, f, repository, unikId);
        }

        private void ConvertAttributes(Element el, XElement f, Repository repository, string unikId)
        {
            //arv først alle arvede attributter
            foreach (Connector connector in el.Connectors)
            {
                //
                if (connector.MetaType == "Generalization")
                {
                    Element elm = repository.GetElementByID(connector.SupplierID);
                    if (el.Name != elm.Name) //Kan ikke arve seg selv
                    {
                        LogDebug("Arver: " + elm.Name + " på " + el.Name);
                        ConvertAttributes(elm, f, repository, unikId);

                    }
                }
            }

            foreach (global::EA.Attribute att in el.Attributes)
            {


                try
                {
                    LogDebug("Attributt: " + att.Name + " på " + el.Name);
                    //
                    Boolean kjentType = false;
                    foreach (KjentType kt in KjenteTyper)
                    {
                        if (kt.Navn == att.Type)
                        {
                            kjentType = true;
                            //
                            string verdi = "kjenttype TODO";
                            if (kt.Datatype == "CharacterString") verdi = "Lorem ipsum";
                            if (kt.Datatype == "Real")
                            {
                                Random rnd = new Random();
                                double dbl = rnd.NextDouble();
                                verdi = dbl.ToString();
                            }
                            if (kt.Navn == "Link") verdi = "http://kartverket.no/Standarder/SOSI/";
                            if (kt.Navn == "Organisasjonsnummer") verdi = "87654321";
                            addAttributes(f, att, verdi);
                        }
                    }
                    if (kjentType)
                    {
                        //Alt utført
                    }


                        // Typene i xsd-"format": http://www.arkitektum.no/files/sosi/StandardMapEntries_sosi.xml

                    else if (att.Type.ToLower() == "integer")
                    {
                        Random rnd = new Random();
                        int integer = rnd.Next(1, 13);

                        addAttributes(f, att, integer);
                    }
                    else if (att.Type.ToLower() == "characterstring")
                    {
                        string verdi = "Lorem ipsum";
                        if (att.Name.ToLower() == "lokalid") verdi = unikId;

                        addAttributes(f, att, verdi);
                    }
                    else if (att.Type.ToLower() == "real") // double
                    {
                        Random rnd = new Random();
                        double dbl = rnd.NextDouble();
                        addAttributes(f, att, dbl);
                    }
                    else if (att.Type.ToLower() == "date")
                    {

                        addAttributes(f, att, DateTime.Now.ToString("yyyy-MM-dd"));
                    }
                    else if (att.Type.ToLower() == "datetime")
                    {

                        addAttributes(f, att, DateTime.Now.ToLocalTime());
                    }
                    else if (att.Type.ToLower() == "boolean")
                    {
                        addAttributes(f, att, "true");
                    }

                    else if (att.Type.ToLower() == "flate") // gml:SurfacePropertyType
                    {
                        addSurface(f, att);
                    }
                    else if (att.Type.ToLower() == "punkt") // gml:PointPropertyType
                    {
                        addPoint(f, att);
                    }
                    else if (att.Type.ToLower() == "sverm")
                    {
                        addPointCloud(f, att);
                    }
                    else if (att.Type.ToLower() == "kurve")
                    {
                        addCurve(f, att);
                    }
                    else if (att.Type.ToLower() == "gm_solid")
                    {
                        addSolid(f, att);
                    }
                    else if (att.ClassifierID != 0)
                    {
                        Element attel = repository.GetElementByID(att.ClassifierID);
                        if (attel.Stereotype.ToLower() == "codelist" || attel.Stereotype.ToLower() == "enumeration" || attel.Type.ToLower() == "enumeration")
                        {
                            if (attel.Attributes.Count > 0)
                            {
                                Random rnd = new Random();
                                int pos = rnd.Next(0, attel.Attributes.Count - 1);
                                global::EA.Attribute tmp = attel.Attributes.GetAt((short)pos);
                                string verdi = tmp.Default;
                                if (String.IsNullOrEmpty(verdi)) verdi = tmp.Name;
                                XElement a = new XElement(_app + att.Name, verdi);
                                //hvis asDictionary=true legge inn codespace
                                if (GetTaggedValueFromElement(attel, "asDictionary") == "true")
                                {
                                    string kodeliste = GetTaggedValueFromAttribute(att, "defaultCodespace");
                                    if (String.IsNullOrEmpty(kodeliste)) kodeliste = GetTaggedValueFromElement(attel, "codelist");
                                    if (String.IsNullOrEmpty(kodeliste)) kodeliste = "ingen url angitt";
                                    a.Add(new XAttribute("codeSpace", kodeliste));
                                }
                                if (geoUtil.IsMultippel(att.LowerBound + ".." + att.UpperBound))
                                {
                                    for (int i = 0; i < 3; i++)
                                    {
                                        f.Add(a);
                                    }

                                }
                                else
                                {
                                    f.Add(a);
                                }
                            }
                            else
                            {
                                XElement a = new XElement(_app + att.Name, "ingen koder i kodeliste");
                                f.Add(a);
                            }

                        }
                        else
                        {
                            if (geoUtil.IsMultippel(att.LowerBound + ".." + att.UpperBound))
                            {
                                for (int i = 0; i < 3; i++)
                                {
                                    XElement a = new XElement(_app + att.Name);
                                    XElement b = new XElement(_app + attel.Name);
                                    a.Add(b);
                                    f.Add(a);
                                    ConvertAttributes(attel, b, repository, unikId);
                                }

                            }
                            else
                            {
                                XElement a = new XElement(_app + att.Name);
                                XElement b = new XElement(_app + attel.Name);
                                a.Add(b);
                                f.Add(a);
                                ConvertAttributes(attel, b, repository, unikId);
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    Log("FEIL: I objekt " + el.Name + " " + ex.Message);
                }


            }

            foreach (Connector connector in el.Connectors)
            {
                try
                {
                    //BUG kommer ut feil rekkefølge ifht shapechange på noen få tilfeller, har direction noe å si her?

                    //
                    if ((connector.MetaType == "Association" || connector.MetaType == "Aggregation") && connector.Stereotype.ToLower() != "topo")
                    {
                        Element source = repository.GetElementByID(connector.SupplierID);
                        Element destination = repository.GetElementByID(connector.ClientID);
                        if (connector.SupplierID == connector.ClientID) //Selvassosiasjon
                        {
                            addClientEnd(f, repository, unikId, connector, destination);
                            addSupplierEnd(f, repository, unikId, connector, source);
                        }
                        else if (el.Name == source.Name) //Er Supplier siden
                        {
                            addClientEnd(f, repository, unikId, connector, destination);
                        }
                        else //Er destination/Client siden
                        {
                            addSupplierEnd(f, repository, unikId, connector, source);
                        }

                    }
                }
                catch (Exception ex)
                {
                    Log("FEIL: I objekt " + el.Name + " og en av relasjonene " + ex.Message);
                }
            }

        }

        private void addSupplierEnd(XElement f, Repository repository, string unikId, Connector connector, Element source)
        {
            if (!String.IsNullOrEmpty(connector.SupplierEnd.Role) && (connector.SupplierEnd.Navigable == "Navigable" || connector.Direction == "Unspecified")) //pluss navigerbart
            {
                LogDebug("Attributt(rel): " + connector.SupplierEnd.Role + " til " + source.Name);

                if (source.Stereotype.ToLower() == "datatype")
                {
                    if (geoUtil.IsMultippel(connector.SupplierEnd.Cardinality))
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            XElement a2 = new XElement(_app + connector.SupplierEnd.Role);
                            XElement b2 = new XElement(_app + source.Name);
                            a2.Add(b2);
                            f.Add(a2);
                            ConvertAttributes(source, b2, repository, unikId);
                        }

                    }
                    else
                    {
                        XElement a = new XElement(_app + connector.SupplierEnd.Role);
                        XElement b2 = new XElement(_app + source.Name);
                        a.Add(b2);
                        f.Add(a);
                        ConvertAttributes(source, b2, repository, unikId);

                    }

                }
                else
                {
                    if (geoUtil.IsMultippel(connector.SupplierEnd.Cardinality))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            XElement a = new XElement(_app + connector.SupplierEnd.Role, new XAttribute(_xlink + "href", "#TODO_GML_ID_TIL_" + source.Name));
                            f.Add(a);
                        }
                    }
                    else
                    {
                        XElement a = new XElement(_app + connector.SupplierEnd.Role, new XAttribute(_xlink + "href", "#TODO_GML_ID_TIL_" + source.Name));
                        f.Add(a);
                    }
                }

            }
        }

        private void addClientEnd(XElement f, Repository repository, string unikId, Connector connector, Element destination)
        {
            if (!String.IsNullOrEmpty(connector.ClientEnd.Role) && (connector.ClientEnd.Navigable == "Navigable" || connector.Direction == "Unspecified")) //pluss navigerbart
            {
                LogDebug("Attributt(rel): " + connector.ClientEnd.Role + " til " + destination.Name);

                if (destination.Stereotype.ToLower() == "datatype")
                {
                    if (geoUtil.IsMultippel(connector.ClientEnd.Cardinality))
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            XElement a2 = new XElement(_app + connector.ClientEnd.Role);
                            XElement b2 = new XElement(_app + destination.Name);
                            a2.Add(b2);
                            f.Add(a2);

                            ConvertAttributes(destination, b2, repository, unikId);
                        }

                    }
                    else
                    {
                        XElement a = new XElement(_app + connector.ClientEnd.Role);
                        XElement b2 = new XElement(_app + destination.Name);
                        a.Add(b2);
                        f.Add(a);
                        ConvertAttributes(destination, b2, repository, unikId);

                    }

                }
                else
                {
                    if (geoUtil.IsMultippel(connector.ClientEnd.Cardinality))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            XElement a = new XElement(_app + connector.ClientEnd.Role, new XAttribute(_xlink + "href", "#TODO_GML_ID_TIL_" + destination.Name));
                            f.Add(a);
                        }
                    }
                    else
                    {
                        XElement a = new XElement(_app + connector.ClientEnd.Role, new XAttribute(_xlink + "href", "#TODO_GML_ID_TIL_" + destination.Name));
                        f.Add(a);
                    }
                }

            }
        }

        private void addAttributes(XElement f, global::EA.Attribute att, object verdi)
        {
            if (geoUtil.IsMultippel(att.LowerBound + ".." + att.UpperBound))
            {
                for (int i = 0; i < 3; i++)
                {
                    XElement a = new XElement(_app + att.Name, verdi);
                    f.Add(a);
                }

            }
            else
            {
                XElement a = new XElement(_app + att.Name, verdi);
                f.Add(a);
            }
        }

        private void addSolid(XElement f, string name)
        {
            XElement a = new XElement(_app + name, "volum TODO");
            f.Add(a);
        }

        private void addPointCloud(XElement f, string name)
        {
            XElement a = new XElement(_app + name, "sverm TODO");
            f.Add(a);
        }

        private void addCurve(XElement f, string name) //global::EA.Attribute att)
        {
            if (_settings.use2DGeometry)
            {
                XElement a = new XElement(_app + name, new XElement(_gml + "Curve",
                    new XAttribute(_gml + "id", "geom_" + Guid.NewGuid().ToString()), new XAttribute("srsName", "urn:ogc:def:crs:EPSG::4258"), new XAttribute("srsDimension", "2"),
                    new XElement(_gml + "segments",
                         new XElement(_gml + "LineStringSegment",
                            new XElement(_gml + "posList", "10.016075484076167 59.803969508834115 10.015777901464215 59.803942980224946 10.015542760629383 59.803941742222143 10.015537384546771 59.803940795895215 10.015471906859473 59.803927022739828 10.015394270574596 59.803911996357705")))));
                f.Add(a);

            }
            else
            {
                XElement a = new XElement(_app + name, new XElement(_gml + "LineString",
                    new XAttribute(_gml + "id", "geom_" + Guid.NewGuid().ToString()), new XAttribute("srsName", "urn:ogc:def:crs:EPSG::5972"), new XAttribute("srsDimension", "3"),
                        new XElement(_gml + "posList", "592109.83 6905785.12 498.79 592109.57 6905786.77 498.79 592141.09 6905824.6 498.87 592142.76 6905824.63 498.87")));
                f.Add(a);
            }
        }

        private void addPoint(XElement f, string name)
        {
            if (_settings.use2DGeometry)
            {
                XElement a = new XElement(_app + name, new XElement(_gml + "Point",
                    new XAttribute(_gml + "id", "geom_" + Guid.NewGuid().ToString()), new XAttribute("srsName", "urn:ogc:def:crs:EPSG::4258"), new XAttribute("srsDimension", "2"),
                        new XElement(_gml + "pos", "10.016075484076167 59.803969508834115")));
                f.Add(a);

            }
            else
            {
                XElement a = new XElement(_app + name, new XElement(_gml + "Point",
                    new XAttribute(_gml + "id", "geom_" + Guid.NewGuid().ToString()), new XAttribute("srsName", "urn:ogc:def:crs:EPSG::5972"), new XAttribute("srsDimension", "3"),
                        new XElement(_gml + "pos", "592142.76 6905824.63 501.67")));
                f.Add(a);
            }
        }

        private void addSurface(XElement f, string name)
        {
            if (_settings.use2DGeometry)
            {
                XElement a = new XElement(_app + name, new XElement(_gml + "Polygon",
                     new XAttribute(_gml + "id", "geom_" + Guid.NewGuid().ToString()), new XAttribute("srsName", "urn:ogc:def:crs:EPSG::4258"), new XAttribute("srsDimension", "2"),
                             new XElement(_gml + "exterior",
                                 new XElement(_gml + "LinearRing",
                     new XElement(_gml + "posList", "10.012785050559135 59.735925785104442 10.012629542540568 59.735947997682842 10.012294523915813 59.736022242256652 10.012194342658386 59.736032892435851 10.012182163771772 59.736047803779101 10.012182163771772 59.736047803779101 10.012213909890967 59.736079978139323 10.012213909890967 59.736079978139323 10.012232936428846 59.736056213329555 10.012310958256904 59.736048068740466 10.012647564087626 59.735973362877118 10.012812812674152 59.735949728143915 10.012812812674152 59.735949728143915 10.012785050559135 59.735925785104442")))));
                f.Add(a);
            }
            else
            {
                XElement a = new XElement(_app + name, new XElement(_gml + "Surface",
                     new XAttribute(_gml + "id", "geom_" + Guid.NewGuid().ToString()), new XAttribute("srsName", "urn:ogc:def:crs:EPSG::5972"), new XAttribute("srsDimension", "2"),
                     new XElement(_gml + "patches",
                         new XElement(_gml + "PolygonPatch",
                             new XElement(_gml + "exterior",
                                 new XElement(_gml + "LinearRing",
                     new XElement(_gml + "posList", "592015.41 6905848.651 592015.68 6905845.95 592016.461 6905843.48 592017.97 6905840.59 592020.151 6905837.51 592024.32 6905833.031 592028.671 6905828.801 592036.281 6905821.161 592044.87 6905812.71 592045.972 6905811.591 592048.041 6905813.38 592055.464 6905806.69 592052.955 6905804.52 592048.147 6905800.359 592045.58 6905802.83 592035.94 6905812.541 592029.641 6905818.86 592019.36 6905829.1 592014.48 6905833.86 592011.081 6905836.88 592009.69 6905837.95 592008.66 6905838.411 592006.021 6905839.13 592004.35 6905839.311 592002.711 6905839.401 592000.98 6905839.13 591999.591 6905838.641 592017.531 6905855.521 592016.54 6905854.19 592015.86 6905852.641 592015.49 6905850.911 592015.41 6905848.651")))))));
                f.Add(a);
            }
        }

        private new void LogDebug(string message, string category = "System")
        {
            //base.LogDebug(message, "System");
        }
    }
}
