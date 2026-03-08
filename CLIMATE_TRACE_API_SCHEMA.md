# Climate TRACE API Data Schema

## API Endpoint
```
GET https://api.climatetrace.org/v7/sources
```

## Query Parameters
- `limit` (integer): Number of results to return (10-1000)
- `offset` (integer): Pagination offset
- `country` (string): 3-letter country code (e.g., USA, CHN, IND)
- `sector` (string): Sector filter (e.g., power, transportation, fossil-fuel-operations)

## Response Format
The API returns a JSON array of emissions source objects.

## Emissions Source Object Schema

```json
[
  {
    "id": <integer>,
    "name": <string>,
    "sector": <string>,
    "subsector": <string>,
    "country": <string>,
    "assetType": <string>,
    "sourceType": <string>,
    "centroid": {
      "longitude": <float>,
      "latitude": <float>,
      "srid": <integer>
    },
    "gas": <string>,
    "emissionsQuantity": <float>,
    "emissionsFactor": <float>,
    "emissionsFactorUnits": <string>,
    "activity": <float>,
    "activityUnits": <string>,
    "capacity": <float>,
    "capacityUnits": <string>,
    "capacityFactor": <float>,
    "year": <integer>
  }
]
```

## Field Descriptions

| Field | Type | Description |
|-------|------|-------------|
| `id` | integer | Unique identifier for the emissions source |
| `name` | string | Name of the emissions source |
| `sector` | string | Primary sector (e.g., "power", "transportation") |
| `subsector` | string | More specific subsector classification |
| `country` | string | 3-letter country code |
| `assetType` | string | Type of asset generating emissions |
| `sourceType` | string | Classification of the emission source |
| `centroid` | object | Geographic location data |
| `centroid.longitude` | float | Longitude coordinate |
| `centroid.latitude` | float | Latitude coordinate |
| `centroid.srid` | integer | Spatial Reference System Identifier |
| `gas` | string | Type of greenhouse gas (typically CO2e) |
| `emissionsQuantity` | float | Total emissions in tonnes CO2e |
| `emissionsFactor` | float | Emissions factor value |
| `emissionsFactorUnits` | string | Units for emissions factor |
| `activity` | float | Activity level measurement |
| `activityUnits` | string | Units for activity measurement |
| `capacity` | float | Capacity of the source |
| `capacityUnits` | string | Units for capacity measurement |
| `capacityFactor` | float | Utilization factor (0-1 range typically) |
| `year` | integer | Year of the emissions data |

## Example Response
```json
[
  {
    "id": 12345,
    "name": "Sample Power Plant",
    "sector": "power",
    "subsector": "coal",
    "country": "USA",
    "assetType": "power-plant",
    "sourceType": "point",
    "centroid": {
      "longitude": -122.4194,
      "latitude": 37.7749,
      "srid": 4326
    },
    "gas": "co2e_100yr",
    "emissionsQuantity": 1500000.0,
    "emissionsFactor": 0.95,
    "emissionsFactorUnits": "t_co2e_per_mwh",
    "activity": 8760000.0,
    "activityUnits": "mwh",
    "capacity": 1000.0,
    "capacityUnits": "mw",
    "capacityFactor": 0.75,
    "year": 2023
  }
]
```

## Notes
- All emissions quantities are measured in tonnes (t) of CO2 equivalent
- The SRID value 4326 represents the WGS 84 coordinate system (standard GPS coordinates)
- Emissions quantities can range from small values to hundreds of millions of tonnes
- The API endpoint used is version 7 (`/v7/sources`)
