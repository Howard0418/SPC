# Database Schema Summary

## Core Entities

### Products
- `Id` (PK), `ProductCode` (Unique), `ProductName`, `IsActive`.

### Stations
- `Id` (PK), `StationCode` (Unique), `StationName`, `IsActive`.

### InspectionItems
- `Id` (PK), `ItemCode` (Unique), `ItemName`.
- `DataType` (Numeric, Text, Boolean).
- `Usl`, `Lsl` (Specification Limits).
- `Ucl`, `Lcl` (Control Limits).
- `TargetValue`, `Unit`.

### ProductStationItems (Relationship)
- `ProductId`, `StationId`, `InspectionItemId`.
- `SampleSize` (n for SPC sub-grouping).

## Transactional Data

### MeasurementBatches
- `Id` (PK), `BatchNo`, `ProductId`, `StationId`, `MeasuredAt`, `SourceType` (Manual/Csv).

### MeasurementValues
- `Id` (PK), `BatchId` (FK), `InspectionItemId`, `SampleNo`.
- `ValueNumeric`, `ValueText`, `ValueBool`.

## Analytics & Logic

### FormulaDefinitions
- `FormulaCode` (AVG, CPK, STDEV, etc.), `Expression`.

### AlertEvents
- `ProductId`, `StationId`, `InspectionItemId`.
- `ActualValue`, `AlertType` (OutOfSpec/OutOfControl).
- `Message`, `IsAcknowledged`.

## Enums
- **DataType**: 1=Numeric, 2=Text, 3=Boolean.
- **SourceType**: 1=Manual, 2=Csv.
- **AlertType**: 1=OutOfSpec, 2=OutOfControl.
