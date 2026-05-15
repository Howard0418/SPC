using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class V4_EnterpriseMasterDataAndAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UploadErrorId",
                table: "UploadErrors",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "UploadDetailId",
                table: "UploadDetails",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "RuleId",
                table: "SpcRules",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "RuleGroupId",
                table: "SpcRuleGroups",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "SpcResultId",
                table: "SpcCalculationResults",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "CharacteristicId",
                table: "QualityCharacteristics",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "ProcessId",
                table: "Processes",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "PartId",
                table: "Parts",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "MachineId",
                table: "Machines",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "ChartTypeId",
                table: "ControlChartTypes",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "ChartGroupId",
                table: "ControlChartGroups",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "ChartCategoryId",
                table: "ControlChartCategories",
                newName: "Id");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "WorkOrders",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "WorkOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "WorkOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "WorkOrders",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "WorkOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "VariableMeasurements",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "VariableMeasurements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "VariableMeasurements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "VariableMeasurements",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "VariableMeasurements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "VariableMeasurements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "UploadErrors",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "UploadErrors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "UploadErrors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "UploadErrors",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "UploadErrors",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "UploadErrors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "UploadDetails",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "UploadDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "UploadDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "UploadDetails",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "UploadDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "UploadDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "UploadBatches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "UploadBatches",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "UploadBatches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "UploadBatches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Stations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Stations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Stations",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Stations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Stations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "StationOperationSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "StationOperationSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "StationOperationSessions",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "StationOperationSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "StationOperationSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "SpcRules",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "SpcRules",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SpcRules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SpcRules",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "SpcRules",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "SpcRules",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "SpcRuleGroups",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "SpcRuleGroups",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SpcRuleGroups",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SpcRuleGroups",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "SpcRuleGroups",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "SpcRuleGroups",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "SpcCalculationResults",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "SpcCalculationResults",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SpcCalculationResults",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SpcCalculationResults",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "SpcCalculationResults",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "SpcCalculationResults",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "QualityCharacteristics",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "QualityCharacteristics",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "QualityCharacteristics",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "QualityCharacteristics",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "QualityCharacteristics",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "QualityCharacteristics",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ProductStationItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ProductStationItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ProductStationItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ProductStationItems",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ProductStationItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "ProductStationItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Products",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Processes",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Processes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Processes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Processes",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Processes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Processes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Parts",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Parts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Parts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Parts",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Parts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "PartProcessCharacteristics",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "PartProcessCharacteristics",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PartProcessCharacteristics",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PartProcessCharacteristics",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "PartProcessCharacteristics",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "PartProcessCharacteristics",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "MeasurementValues",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "MeasurementValues",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "MeasurementValues",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "MeasurementValues",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "MeasurementValues",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "MeasurementBatches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "MeasurementBatches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "MeasurementBatches",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "MeasurementBatches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "MeasurementBatches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Machines",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Machines",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Machines",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Machines",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Machines",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Machines",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "InspectionItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "InspectionItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "InspectionItems",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "InspectionItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "InspectionItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "FormulaDefinitions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "FormulaDefinitions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "FormulaDefinitions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "FormulaDefinitions",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "FormulaDefinitions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "FormulaDefinitions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ControlChartTypes",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ControlChartTypes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ControlChartTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ControlChartTypes",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ControlChartTypes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "ControlChartTypes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ControlChartGroups",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ControlChartGroups",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ControlChartGroups",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ControlChartGroups",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ControlChartGroups",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "ControlChartGroups",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ControlChartCategories",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ControlChartCategories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ControlChartCategories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ControlChartCategories",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ControlChartCategories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "ControlChartCategories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "AttributeMeasurements",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "AttributeMeasurements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AttributeMeasurements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AttributeMeasurements",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "AttributeMeasurements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "AttributeMeasurements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "AlertEvents",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "AlertEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AlertEvents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AlertEvents",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "AlertEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Factories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlantId = table.Column<int>(type: "int", nullable: false),
                    FactoryCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FactoryName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Factories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Operators",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OperatorCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OperatorName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Department = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Operators", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Plants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlantCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PlantName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductionLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FactoryId = table.Column<int>(type: "int", nullable: false),
                    LineCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LineName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionLines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Shifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShiftCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ShiftName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shifts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SupplierName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnitCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UnitName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CustomerCode",
                table: "Customers",
                column: "CustomerCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Factories_FactoryCode",
                table: "Factories",
                column: "FactoryCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Operators_OperatorCode",
                table: "Operators",
                column: "OperatorCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Plants_PlantCode",
                table: "Plants",
                column: "PlantCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionLines_LineCode",
                table: "ProductionLines",
                column: "LineCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_ShiftCode",
                table: "Shifts",
                column: "ShiftCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_SupplierCode",
                table: "Suppliers",
                column: "SupplierCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Units_UnitCode",
                table: "Units",
                column: "UnitCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Factories");

            migrationBuilder.DropTable(
                name: "Operators");

            migrationBuilder.DropTable(
                name: "Plants");

            migrationBuilder.DropTable(
                name: "ProductionLines");

            migrationBuilder.DropTable(
                name: "Shifts");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Units");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "VariableMeasurements");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "VariableMeasurements");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "VariableMeasurements");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "VariableMeasurements");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "VariableMeasurements");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "VariableMeasurements");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "UploadErrors");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "UploadErrors");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "UploadErrors");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "UploadErrors");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "UploadErrors");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "UploadErrors");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "UploadDetails");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "UploadDetails");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "UploadDetails");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "UploadDetails");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "UploadDetails");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "UploadDetails");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "UploadBatches");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "UploadBatches");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "UploadBatches");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "UploadBatches");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Stations");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Stations");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Stations");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Stations");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Stations");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "StationOperationSessions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "StationOperationSessions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "StationOperationSessions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "StationOperationSessions");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "StationOperationSessions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "SpcRules");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "SpcRules");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SpcRules");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SpcRules");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "SpcRules");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "SpcRules");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "SpcRuleGroups");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "SpcRuleGroups");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SpcRuleGroups");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SpcRuleGroups");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "SpcRuleGroups");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "SpcRuleGroups");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "SpcCalculationResults");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "SpcCalculationResults");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SpcCalculationResults");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SpcCalculationResults");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "SpcCalculationResults");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "SpcCalculationResults");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "QualityCharacteristics");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "QualityCharacteristics");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "QualityCharacteristics");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "QualityCharacteristics");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "QualityCharacteristics");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "QualityCharacteristics");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ProductStationItems");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ProductStationItems");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ProductStationItems");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ProductStationItems");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ProductStationItems");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "ProductStationItems");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Processes");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Processes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Processes");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Processes");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Processes");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Processes");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PartProcessCharacteristics");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "PartProcessCharacteristics");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PartProcessCharacteristics");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PartProcessCharacteristics");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "PartProcessCharacteristics");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "PartProcessCharacteristics");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "MeasurementValues");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "MeasurementValues");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "MeasurementValues");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "MeasurementValues");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "MeasurementValues");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "MeasurementBatches");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "MeasurementBatches");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "MeasurementBatches");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "MeasurementBatches");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "MeasurementBatches");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "InspectionItems");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "InspectionItems");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "InspectionItems");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "InspectionItems");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "InspectionItems");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "FormulaDefinitions");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "FormulaDefinitions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "FormulaDefinitions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "FormulaDefinitions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "FormulaDefinitions");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "FormulaDefinitions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ControlChartTypes");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ControlChartTypes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ControlChartTypes");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ControlChartTypes");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ControlChartTypes");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "ControlChartTypes");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ControlChartGroups");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ControlChartGroups");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ControlChartGroups");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ControlChartGroups");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ControlChartGroups");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "ControlChartGroups");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ControlChartCategories");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ControlChartCategories");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ControlChartCategories");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ControlChartCategories");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ControlChartCategories");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "ControlChartCategories");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AttributeMeasurements");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "AttributeMeasurements");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AttributeMeasurements");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AttributeMeasurements");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AttributeMeasurements");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "AttributeMeasurements");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AlertEvents");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "AlertEvents");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AlertEvents");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AlertEvents");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "AlertEvents");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "UploadErrors",
                newName: "UploadErrorId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "UploadDetails",
                newName: "UploadDetailId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "SpcRules",
                newName: "RuleId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "SpcRuleGroups",
                newName: "RuleGroupId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "SpcCalculationResults",
                newName: "SpcResultId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "QualityCharacteristics",
                newName: "CharacteristicId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Processes",
                newName: "ProcessId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Parts",
                newName: "PartId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Machines",
                newName: "MachineId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ControlChartTypes",
                newName: "ChartTypeId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ControlChartGroups",
                newName: "ChartGroupId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ControlChartCategories",
                newName: "ChartCategoryId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "WorkOrders",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Parts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
