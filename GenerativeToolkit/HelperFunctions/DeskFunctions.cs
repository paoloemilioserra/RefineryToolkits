﻿using Autodesk.DesignScript.Geometry;
using Autodesk.DesignScript.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Revit.Elements;

namespace GenerativeToolkit.HelperFunctions
{
    class DeskFunctions
    {
        #region Get room boundaries as Polygons
        [IsVisibleInDynamoLibrary(false)]
        public static List<Polygon> PolygonsFromRooms(List<Room> rooms)
        {
            rooms = FilterRooms(rooms);
            List<Polygon> boundaryPolygon = new List<Polygon>();
            foreach (Room room in rooms)
            {
                IEnumerable<IEnumerable<Curve>> segments = room.CoreBoundary;
                boundaryPolygon.AddRange(BoundaryPolygon(segments));
            }
            List<Polygon> roomPolygons = CombineIntersectingRooms(boundaryPolygon);
            boundaryPolygon.ForEach(x => x.Dispose());

            return roomPolygons;
        }
        #endregion

        #region Filter unplaced Rooms 
        [IsVisibleInDynamoLibrary(false)]
        private static List<Room> FilterRooms(List<Room> rooms)
        {
            List<Room> filteredRooms = new List<Room>();
            foreach (Room room in rooms)
            {
                double roomArea = room.Area;
                if (roomArea > 0)
                {
                    filteredRooms.Add(room);
                }
            }
            return filteredRooms;
        }
        #endregion

        #region Get all rooms Boundary Polygon
        [IsVisibleInDynamoLibrary(false)]
        private static List<Polygon> BoundaryPolygon(IEnumerable<IEnumerable<Curve>> boundarySegments)
        {
            List<Polygon> polygons = new List<Polygon>();  
            foreach (IList<Curve> segmentList in boundarySegments)
            {          
                List<Point> boundaryPoints = new List<Point>();  
                foreach (Curve segment in segmentList)
                {
                    double x = segment.StartPoint.X;
                    double y = segment.StartPoint.Y;
                    boundaryPoints.Add(Point.ByCoordinates(x, y));
                    segment.Dispose();
                }
                polygons.Add(Polygon.ByPoints(boundaryPoints));
                boundaryPoints.ForEach(x => x.Dispose());
            }

            return polygons;
        }
        #endregion

        #region Combine intersecting rooms only seperated with a room seperation line
        [IsVisibleInDynamoLibrary(false)]
        private static List<Polygon> CombineIntersectingRooms(List<Polygon> polygons)
        {
            Plane plane = Plane.ByOriginNormal(Point.ByCoordinates(0, 0, 0), Vector.ByCoordinates(0, 0, 1));
            List<Solid> solidSurfs = new List<Solid>();
            foreach (Polygon polygon in polygons)
            {
                Solid solid = Surface.ByPatch(polygon).Thicken(1);
                solidSurfs.Add(solid);
            }
            Solid solidUnion = Solid.ByUnion(solidSurfs);
            Autodesk.DesignScript.Geometry.Geometry[] roomSurfs = solidUnion.Intersect(plane);

            List<Polygon> spacePolygons = new List<Polygon>();
            foreach (Surface surf in roomSurfs)
            {
                Curve[] perimeterCrvs = surf.PerimeterCurves();
                List<Point> polygonPoints = new List<Point>();
                foreach (Curve crv in perimeterCrvs)
                {
                    polygonPoints.Add(crv.StartPoint);
                    crv.Dispose();
                }
                spacePolygons.Add(Polygon.ByPoints(polygonPoints));
                polygonPoints.ForEach(x => x.Dispose());
                surf.Dispose();
            }

            // Dispose Dynamo Geometry
            solidSurfs.ForEach(x => x.Dispose());
            solidUnion.Dispose();
            plane.Dispose();

            return spacePolygons;
        }
        #endregion
    }
}
