using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Kartverket.Generators
{
    class SampleGmlDataGenerator : SampleDataGenerator
    {
        private GmlSettings _gmlSettings;
        private XNamespace _targetNamespace;
        private XNamespace _xmlns_gml;


        public SampleGmlDataGenerator(GmlSettings gmlSettings, XNamespace targetNamespace, XNamespace xmlns_gml)
            : base()
        {
            _gmlSettings = gmlSettings;
            _targetNamespace = targetNamespace;
            _xmlns_gml = xmlns_gml;
        }

        public new bool SupportsType(string type)
        {
            return GenerateForType(type) != null || base.GenerateForType(type) != null;
        }


        public new object GenerateForType(string type)
        {
            if (base.SupportsType(type)) return base.GenerateForType(type);

            switch (type)
            {
                case "gml:SurfacePropertyType": return GenerateSurface();
                case "gml:PointPropertyType": return GeneratePoint();
                case "gml:MultiPointPropertyType": return GenerateMultiPoint();
                case "gml:CurvePropertyType": return GenerateCurve();

                default: return null;
            }
        }

        private object GenerateSurface()
        {
            return (_gmlSettings.Use3DGeometry) ? Generate3DSurface() : Generate2DSurface();
        }

        private object Generate2DSurface()
        {
              XElement surface = new XElement(_xmlns_gml + "Polygon",
                        new XAttribute(_xmlns_gml + "id", "geom_" + Guid.NewGuid().ToString()),
                        new XAttribute("srsName", "urn:ogc:def:crs:EPSG::4258"),
                        new XAttribute("srsDimension", "2"),
                            new XElement(_xmlns_gml + "exterior",
                                new XElement(_xmlns_gml + "LinearRing",
                                    new XElement(_xmlns_gml + "posList", "10.012785050559135 59.735925785104442 10.012629542540568 59.735947997682842 10.012294523915813 59.736022242256652 10.012194342658386 59.736032892435851 10.012182163771772 59.736047803779101 10.012182163771772 59.736047803779101 10.012213909890967 59.736079978139323 10.012213909890967 59.736079978139323 10.012232936428846 59.736056213329555 10.012310958256904 59.736048068740466 10.012647564087626 59.735973362877118 10.012812812674152 59.735949728143915 10.012812812674152 59.735949728143915 10.012785050559135 59.735925785104442"))));

            return surface;
        }

        private object Generate3DSurface()
        {
            XElement surface = new XElement(_xmlns_gml + "Surface",
                    new XAttribute(_xmlns_gml + "id", "geom_" + Guid.NewGuid().ToString()),
                    new XAttribute("srsName", "urn:ogc:def:crs:EPSG::5972"),
                    new XAttribute("srsDimension", "2"),
                new XElement(_xmlns_gml + "patches",
                    new XElement(_xmlns_gml + "PolygonPatch",
                        new XElement(_xmlns_gml + "exterior",
                            new XElement(_xmlns_gml + "LinearRing",
                                new XElement(_xmlns_gml + "posList", "592015.41 6905848.651 592015.68 6905845.95 592016.461 6905843.48 592017.97 6905840.59 592020.151 6905837.51 592024.32 6905833.031 592028.671 6905828.801 592036.281 6905821.161 592044.87 6905812.71 592045.972 6905811.591 592048.041 6905813.38 592055.464 6905806.69 592052.955 6905804.52 592048.147 6905800.359 592045.58 6905802.83 592035.94 6905812.541 592029.641 6905818.86 592019.36 6905829.1 592014.48 6905833.86 592011.081 6905836.88 592009.69 6905837.95 592008.66 6905838.411 592006.021 6905839.13 592004.35 6905839.311 592002.711 6905839.401 592000.98 6905839.13 591999.591 6905838.641 592017.531 6905855.521 592016.54 6905854.19 592015.86 6905852.641 592015.49 6905850.911 592015.41 6905848.651"))))));

            return surface;
        }

        private object GeneratePoint()
        {
            return (_gmlSettings.Use3DGeometry) ? Generate3DPoint() : Generate2DPoint();
        }

        private object Generate2DPoint()
        {
             XElement point = new XElement(_xmlns_gml + "Point",
                    new XAttribute(_xmlns_gml + "id", "geom_" + Guid.NewGuid().ToString()), new XAttribute("srsName", "urn:ogc:def:crs:EPSG::4258"), new XAttribute("srsDimension", "2"),
                        new XElement(_xmlns_gml + "pos", "10.016075484076167 59.803969508834115"));
            return point;

        }

        private object Generate3DPoint()
        {
                XElement point = new XElement(_xmlns_gml + "Point",
                    new XAttribute(_xmlns_gml + "id", "geom_" + Guid.NewGuid().ToString()), new XAttribute("srsName", "urn:ogc:def:crs:EPSG::5972"), new XAttribute("srsDimension", "3"),
                        new XElement(_xmlns_gml + "pos", "592142.76 6905824.63 501.67"));
            return point;
        }

        private object GenerateMultiPoint()
        {
            return null;
        }

        private object GenerateCurve()
        {
            return (_gmlSettings.Use3DGeometry) ? Generate3DCurve() : Generate2DCurve();
        }

        private object Generate2DCurve()
        {
                XElement curve = new XElement(_xmlns_gml + "Curve",
                new XAttribute(_xmlns_gml + "id", "geom_" + Guid.NewGuid().ToString()),
                new XAttribute("srsName", "urn:ogc:def:crs:EPSG::4258"),
                new XAttribute("srsDimension", "2"),
                    new XElement(_xmlns_gml + "segments",
                    new XElement(_xmlns_gml + "LineStringSegment",
                    new XElement(_xmlns_gml + "posList", "10.016075484076167 59.803969508834115 10.015777901464215 59.803942980224946 10.015542760629383 59.803941742222143 10.015537384546771 59.803940795895215 10.015471906859473 59.803927022739828 10.015394270574596 59.803911996357705"))));
            return curve;
        }

        private object Generate3DCurve()
        {
              XElement curve = new XElement(_xmlns_gml + "LineString",
                new XAttribute(_xmlns_gml + "id", "geom_" + Guid.NewGuid().ToString()),
                new XAttribute("srsName", "urn:ogc:def:crs:EPSG::5972"),
                new XAttribute("srsDimension", "3"),
                    new XElement(_xmlns_gml + "posList", "592109.83 6905785.12 498.79 592109.57 6905786.77 498.79 592141.09 6905824.6 498.87 592142.76 6905824.63 498.87"));
            return curve;
        }
    }
}
