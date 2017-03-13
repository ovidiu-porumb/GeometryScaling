using System;
using System.Collections.Generic;
using System.Linq;
using GeoJSON.Net.Geometry;
using Microsoft.SqlServer.Types;

namespace OP.GeometryScaling.Core
{
    public class GeometryScaling
    {
        private double _earthsRadiusInMeters = 6378137;

        public SqlGeography ScaleDownMultiPolygon(MultiPolygon multiPolygon, double factor)
        {
            List<MultiPolygon> shiftedMultiPolygonsAroundTheAxis = GetShiftedMultipolygonsAroundTheAxis(multiPolygon, factor);
            List<SqlGeography> shiftedSqlGeographies = ConvertToSqlGeographies(shiftedMultiPolygonsAroundTheAxis);
            var originalSqlGeography = multiPolygon.FromGeoJsonToSqlGeography();

            SqlGeography result = originalSqlGeography;
            foreach (var shiftedSqlGeography in shiftedSqlGeographies)
            {
                result = result.STDifference(shiftedSqlGeography);
            }

            return result;
        }

        private List<MultiPolygon> GetShiftedMultipolygonsAroundTheAxis(MultiPolygon multiPolygon, double factor)
        {
            var shftingTable = new List<ShiftingFactorForCoordinates>
            {
                new ShiftingFactorForCoordinates
                {
                    ShiftingFactorForX = factor,
                    ShiftingFactorForY = 0
                },
                new ShiftingFactorForCoordinates
                {
                    ShiftingFactorForX = 0,
                    ShiftingFactorForY = factor
                },
                new ShiftingFactorForCoordinates
                {
                    ShiftingFactorForX = -factor,
                    ShiftingFactorForY = 0
                },
                new ShiftingFactorForCoordinates
                {
                    ShiftingFactorForX = 0,
                    ShiftingFactorForY = -factor
                }
            };

            return shftingTable.Select(shiftingOption => GetShiftedMultpolygon(multiPolygon, shiftingOption)).ToList();
        }

        private MultiPolygon GetShiftedMultpolygon(MultiPolygon multipolygon, ShiftingFactorForCoordinates shiftingOption)
        {
            List<LineString> lineStrings = multipolygon.Coordinates.SelectMany(polygon => polygon.Coordinates).ToList();

            List<LineString> transformedLineStrings = new List<LineString>();
            foreach (LineString line in lineStrings)
            {
                var updatedCoords = new List<IPosition>();
                foreach (IPosition coords in line.Coordinates)
                {
                    GeographicPosition geographicPosition = (GeographicPosition)coords;
                    var latitudeFactor = (shiftingOption.ShiftingFactorForY / _earthsRadiusInMeters) * 180 / Math.PI;
                    var longitudeFactor = (shiftingOption.ShiftingFactorForX / _earthsRadiusInMeters) * (180 / Math.PI) /
                                          Math.Cos(geographicPosition.Latitude * Math.PI / 180);

                    updatedCoords.Add(
                        new GeographicPosition(
                            geographicPosition.Latitude + latitudeFactor,
                            geographicPosition.Longitude + longitudeFactor));
                }

                transformedLineStrings.Add(new LineString(updatedCoords));
            }

            var result = new MultiPolygon(new List<Polygon> { new Polygon(transformedLineStrings) });
            return result;
        }

        private List<SqlGeography> ConvertToSqlGeographies(List<MultiPolygon> shiftedMultiPolygonsAroundTheAxis)
        {
            var result = new List<SqlGeography>();

            foreach (var multipolygon in shiftedMultiPolygonsAroundTheAxis)
            {
                result.Add(multipolygon.FromGeoJsonToSqlGeography());
            }

            return result;
        }
    }
}
