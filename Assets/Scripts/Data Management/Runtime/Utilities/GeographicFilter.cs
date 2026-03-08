using System;
using System.Collections.Generic;
using UnityEngine;

namespace Data_Management.Runtime.Utilities
{
    /// <summary>
    /// Utility class for geographic filtering and distance calculations.
    /// Provides reusable methods for filtering collections by location.
    /// </summary>
    public static class GeographicFilter
    {
        /// <summary>
        /// Filter a collection of items by distance from a center point.
        /// Generic method that works with any type - you provide a function to extract coordinates.
        /// </summary>
        /// <typeparam name="T">The type of items to filter</typeparam>
        /// <param name="items">The collection to filter</param>
        /// <param name="centerLat">Center latitude</param>
        /// <param name="centerLon">Center longitude</param>
        /// <param name="radiusKm">Radius in kilometers</param>
        /// <param name="getCoordinates">Function to extract coordinates from each item. Return null to exclude item.</param>
        /// <returns>Filtered list containing only items within the radius</returns>
        public static List<T> FilterByDistance<T>(
            List<T> items,
            float centerLat,
            float centerLon,
            float radiusKm,
            Func<T, (float lat, float lon)?> getCoordinates)
        {
            var filtered = new List<T>();

            foreach (var item in items)
            {
                var coords = getCoordinates(item);

                // Skip items without valid coordinates
                if (!coords.HasValue)
                {
                    continue;
                }

                float distance = CalculateDistance(
                    centerLat, centerLon,
                    coords.Value.lat, coords.Value.lon);

                if (distance <= radiusKm)
                {
                    filtered.Add(item);
                }
            }

            return filtered;
        }

        /// <summary>
        /// Calculate the great-circle distance between two points on Earth using Haversine formula.
        /// </summary>
        /// <param name="lat1">Latitude of first point (degrees)</param>
        /// <param name="lon1">Longitude of first point (degrees)</param>
        /// <param name="lat2">Latitude of second point (degrees)</param>
        /// <param name="lon2">Longitude of second point (degrees)</param>
        /// <returns>Distance in kilometers</returns>
        public static float CalculateDistance(float lat1, float lon1, float lat2, float lon2)
        {
            const float EARTH_RADIUS_KM = 6371f;

            // Convert degrees to radians
            float dLat = Mathf.Deg2Rad * (lat2 - lat1);
            float dLon = Mathf.Deg2Rad * (lon2 - lon1);
            float lat1Rad = Mathf.Deg2Rad * lat1;
            float lat2Rad = Mathf.Deg2Rad * lat2;

            // Haversine formula
            float a = Mathf.Sin(dLat / 2) * Mathf.Sin(dLat / 2) +
                      Mathf.Cos(lat1Rad) * Mathf.Cos(lat2Rad) *
                      Mathf.Sin(dLon / 2) * Mathf.Sin(dLon / 2);

            float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));

            return EARTH_RADIUS_KM * c;
        }

        /// <summary>
        /// Check if a point is within a radius of a center point.
        /// Convenience method for simple distance checks.
        /// </summary>
        /// <returns>True if point is within radius</returns>
        public static bool IsWithinRadius(
            float centerLat, float centerLon,
            float pointLat, float pointLon,
            float radiusKm)
        {
            float distance = CalculateDistance(centerLat, centerLon, pointLat, pointLon);
            return distance <= radiusKm;
        }
    }
}
