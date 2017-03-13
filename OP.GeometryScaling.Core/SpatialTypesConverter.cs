using System;
using System.Data.SqlTypes;
using GeoJSON.Net.Contrib.MsSqlSpatial;
using GeoJSON.Net.Geometry;
using Microsoft.SqlServer.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace OP.GeometryScaling.Core
{
    public static class SpatialTypesConverter
    {
        public static SqlGeography FromGeoJsonToSqlGeography(this MultiPolygon multiPolygon)
        {
            SqlGeography sqlGeography = multiPolygon.ToSqlGeography();

            if (sqlGeography.STIsValid() == SqlBoolean.False)
            {
                //ErrorSignal.FromCurrentContext().Raise(new InvalidMultipolygonDefinitionException());

                SqlGeography validSqlGeography = sqlGeography.MakeValid();
                return validSqlGeography;
            }

            return sqlGeography;
        }

        public static string FromSqlGeographyToGeoJsonString(this SqlGeography sqlMultipolygon)
        {
            string result;

            if (sqlMultipolygon.InstanceOf("MULTIPOLYGON") == SqlBoolean.True)
            {
                try
                {
                    var geoJsonMultipolygon = sqlMultipolygon.ToGeoJSONObject<MultiPolygon>();
                    geoJsonMultipolygon.BoundingBoxes = null;

                    result = JsonConvert.SerializeObject(geoJsonMultipolygon,
                        new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
                }
                catch (Exception)
                {
                    var geoJsonMultipolygon = sqlMultipolygon.ToGeoJSONObject<Polygon>();
                    result = JsonConvert.SerializeObject(geoJsonMultipolygon,
                        new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
                }
            }
            else
            {
                try
                {
                    var geoJsonMultipolygon = sqlMultipolygon.ToGeoJSONObject<GeometryCollection>();
                    geoJsonMultipolygon.BoundingBoxes = null;

                    result = JsonConvert.SerializeObject(geoJsonMultipolygon,
                        new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
                }
                catch (Exception)
                {
                    var geoJsonMultipolygon = sqlMultipolygon.ToGeoJSONObject<Polygon>();
                    result = JsonConvert.SerializeObject(geoJsonMultipolygon,
                        new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
                }
            }

            return result;
        }
    }
}
