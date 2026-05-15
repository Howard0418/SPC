using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqlServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlertEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    StationId = table.Column<int>(type: "int", nullable: false),
                    InspectionItemId = table.Column<int>(type: "int", nullable: false),
                    ActualValue = table.Column<double>(type: "float", nullable: true),
                    AlertType = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BatchId = table.Column<int>(type: "int", nullable: true),
                    MeasurementValueId = table.Column<int>(type: "int", nullable: true),
                    IsAcknowledged = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RootCause = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrectiveAction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponsibleUser = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ControlChartGroups",
                columns: table => new
                {
                    ChartGroupId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControlChartGroups", x => x.ChartGroupId);
                });

            migrationBuilder.CreateTable(
                name: "FormulaDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FormulaCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Expression = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsBuiltIn = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormulaDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InspectionItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataType = table.Column<int>(type: "int", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Usl = table.Column<double>(type: "float", nullable: true),
                    Lsl = table.Column<double>(type: "float", nullable: true),
                    Ucl = table.Column<double>(type: "float", nullable: true),
                    Lcl = table.Column<double>(type: "float", nullable: true),
                    TargetValue = table.Column<double>(type: "float", nullable: true),
                    IsSpcEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MeasurementBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    StationId = table.Column<int>(type: "int", nullable: false),
                    WorkOrderId = table.Column<int>(type: "int", nullable: true),
                    StationOperationSessionId = table.Column<int>(type: "int", nullable: true),
                    LotNo = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SerialNo = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    MeasuredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OperatorName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasurementBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Parts",
                columns: table => new
                {
                    PartId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartNo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PartName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Specification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Customer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parts", x => x.PartId);
                });

            migrationBuilder.CreateTable(
                name: "Processes",
                columns: table => new
                {
                    ProcessId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcessCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProcessName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Processes", x => x.ProcessId);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductStationItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    StationId = table.Column<int>(type: "int", nullable: false),
                    InspectionItemId = table.Column<int>(type: "int", nullable: false),
                    SampleSize = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductStationItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpcRuleGroups",
                columns: table => new
                {
                    RuleGroupId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuleGroupCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RuleGroupName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpcRuleGroups", x => x.RuleGroupId);
                });

            migrationBuilder.CreateTable(
                name: "StationOperationSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkOrderId = table.Column<int>(type: "int", nullable: false),
                    StationId = table.Column<int>(type: "int", nullable: false),
                    LotNo = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SerialNo = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    OperatorName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StationOperationSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StationCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StationName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UploadBatches",
                columns: table => new
                {
                    UploadBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImportStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    ValidRows = table.Column<int>(type: "int", nullable: false),
                    ErrorRows = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadBatches", x => x.UploadBatchId);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkOrderNo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    PlannedQty = table.Column<int>(type: "int", nullable: false),
                    ActualQty = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlannedStartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlannedEndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualStartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualEndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ControlChartCategories",
                columns: table => new
                {
                    ChartCategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChartGroupId = table.Column<int>(type: "int", nullable: false),
                    CategoryCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CategoryName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControlChartCategories", x => x.ChartCategoryId);
                    table.ForeignKey(
                        name: "FK_ControlChartCategories_ControlChartGroups_ChartGroupId",
                        column: x => x.ChartGroupId,
                        principalTable: "ControlChartGroups",
                        principalColumn: "ChartGroupId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeasurementValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<int>(type: "int", nullable: false),
                    InspectionItemId = table.Column<int>(type: "int", nullable: false),
                    SampleNo = table.Column<int>(type: "int", nullable: false),
                    ValueNumeric = table.Column<double>(type: "float", nullable: true),
                    ValueText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValueBool = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasurementValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeasurementValues_MeasurementBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "MeasurementBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Machines",
                columns: table => new
                {
                    MachineId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MachineCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MachineName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcessId = table.Column<int>(type: "int", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Machines", x => x.MachineId);
                    table.ForeignKey(
                        name: "FK_Machines_Processes_ProcessId",
                        column: x => x.ProcessId,
                        principalTable: "Processes",
                        principalColumn: "ProcessId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SpcRules",
                columns: table => new
                {
                    RuleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuleGroupId = table.Column<int>(type: "int", nullable: false),
                    RuleCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RuleName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RuleConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpcRules", x => x.RuleId);
                    table.ForeignKey(
                        name: "FK_SpcRules_SpcRuleGroups_RuleGroupId",
                        column: x => x.RuleGroupId,
                        principalTable: "SpcRuleGroups",
                        principalColumn: "RuleGroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UploadDetails",
                columns: table => new
                {
                    UploadDetailId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UploadBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowNo = table.Column<int>(type: "int", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadDetails", x => x.UploadDetailId);
                    table.ForeignKey(
                        name: "FK_UploadDetails_UploadBatches_UploadBatchId",
                        column: x => x.UploadBatchId,
                        principalTable: "UploadBatches",
                        principalColumn: "UploadBatchId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ControlChartTypes",
                columns: table => new
                {
                    ChartTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChartCategoryId = table.Column<int>(type: "int", nullable: false),
                    ChartTypeCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ChartTypeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataCategory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiredSampleSize = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FormulaConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControlChartTypes", x => x.ChartTypeId);
                    table.ForeignKey(
                        name: "FK_ControlChartTypes_ControlChartCategories_ChartCategoryId",
                        column: x => x.ChartCategoryId,
                        principalTable: "ControlChartCategories",
                        principalColumn: "ChartCategoryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UploadErrors",
                columns: table => new
                {
                    UploadErrorId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UploadBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadDetailId = table.Column<long>(type: "bigint", nullable: true),
                    RowNo = table.Column<int>(type: "int", nullable: true),
                    FieldName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadErrors", x => x.UploadErrorId);
                    table.ForeignKey(
                        name: "FK_UploadErrors_UploadBatches_UploadBatchId",
                        column: x => x.UploadBatchId,
                        principalTable: "UploadBatches",
                        principalColumn: "UploadBatchId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UploadErrors_UploadDetails_UploadDetailId",
                        column: x => x.UploadDetailId,
                        principalTable: "UploadDetails",
                        principalColumn: "UploadDetailId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "QualityCharacteristics",
                columns: table => new
                {
                    CharacteristicId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacteristicCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CharacteristicName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataCategory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultChartTypeId = table.Column<int>(type: "int", nullable: true),
                    IsSpcEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityCharacteristics", x => x.CharacteristicId);
                    table.ForeignKey(
                        name: "FK_QualityCharacteristics_ControlChartTypes_DefaultChartTypeId",
                        column: x => x.DefaultChartTypeId,
                        principalTable: "ControlChartTypes",
                        principalColumn: "ChartTypeId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PartProcessCharacteristics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartId = table.Column<int>(type: "int", nullable: false),
                    ProcessId = table.Column<int>(type: "int", nullable: false),
                    CharacteristicId = table.Column<int>(type: "int", nullable: false),
                    USL = table.Column<double>(type: "float", nullable: true),
                    LSL = table.Column<double>(type: "float", nullable: true),
                    UCL = table.Column<double>(type: "float", nullable: true),
                    CL = table.Column<double>(type: "float", nullable: true),
                    LCL = table.Column<double>(type: "float", nullable: true),
                    TargetValue = table.Column<double>(type: "float", nullable: true),
                    SampleSize = table.Column<int>(type: "int", nullable: false),
                    ChartTypeId = table.Column<int>(type: "int", nullable: true),
                    RuleGroupId = table.Column<int>(type: "int", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartProcessCharacteristics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartProcessCharacteristics_ControlChartTypes_ChartTypeId",
                        column: x => x.ChartTypeId,
                        principalTable: "ControlChartTypes",
                        principalColumn: "ChartTypeId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PartProcessCharacteristics_Parts_PartId",
                        column: x => x.PartId,
                        principalTable: "Parts",
                        principalColumn: "PartId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartProcessCharacteristics_Processes_ProcessId",
                        column: x => x.ProcessId,
                        principalTable: "Processes",
                        principalColumn: "ProcessId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartProcessCharacteristics_QualityCharacteristics_CharacteristicId",
                        column: x => x.CharacteristicId,
                        principalTable: "QualityCharacteristics",
                        principalColumn: "CharacteristicId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartProcessCharacteristics_SpcRuleGroups_RuleGroupId",
                        column: x => x.RuleGroupId,
                        principalTable: "SpcRuleGroups",
                        principalColumn: "RuleGroupId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AttributeMeasurements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UploadBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartId = table.Column<int>(type: "int", nullable: false),
                    ProcessId = table.Column<int>(type: "int", nullable: false),
                    MachineId = table.Column<int>(type: "int", nullable: false),
                    CharacteristicId = table.Column<int>(type: "int", nullable: false),
                    PartProcessCharacteristicId = table.Column<int>(type: "int", nullable: false),
                    LotNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SampleNo = table.Column<int>(type: "int", nullable: false),
                    InspectedQty = table.Column<int>(type: "int", nullable: true),
                    DefectQty = table.Column<int>(type: "int", nullable: true),
                    DefectCount = table.Column<int>(type: "int", nullable: true),
                    UnitCount = table.Column<int>(type: "int", nullable: true),
                    MeasuredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Operator = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttributeMeasurements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttributeMeasurements_PartProcessCharacteristics_PartProcessCharacteristicId",
                        column: x => x.PartProcessCharacteristicId,
                        principalTable: "PartProcessCharacteristics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttributeMeasurements_UploadBatches_UploadBatchId",
                        column: x => x.UploadBatchId,
                        principalTable: "UploadBatches",
                        principalColumn: "UploadBatchId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SpcCalculationResults",
                columns: table => new
                {
                    SpcResultId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UploadBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataCategory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VariableMeasurementId = table.Column<long>(type: "bigint", nullable: true),
                    AttributeMeasurementId = table.Column<long>(type: "bigint", nullable: true),
                    PartProcessCharacteristicId = table.Column<int>(type: "int", nullable: false),
                    ChartTypeId = table.Column<int>(type: "int", nullable: false),
                    RuleGroupId = table.Column<int>(type: "int", nullable: true),
                    StatisticName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatisticValue = table.Column<double>(type: "float", nullable: true),
                    USL = table.Column<double>(type: "float", nullable: true),
                    LSL = table.Column<double>(type: "float", nullable: true),
                    UCL = table.Column<double>(type: "float", nullable: true),
                    CL = table.Column<double>(type: "float", nullable: true),
                    LCL = table.Column<double>(type: "float", nullable: true),
                    IsOutOfSpec = table.Column<bool>(type: "bit", nullable: false),
                    IsOutOfControl = table.Column<bool>(type: "bit", nullable: false),
                    ViolatedRulesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpcCalculationResults", x => x.SpcResultId);
                    table.ForeignKey(
                        name: "FK_SpcCalculationResults_ControlChartTypes_ChartTypeId",
                        column: x => x.ChartTypeId,
                        principalTable: "ControlChartTypes",
                        principalColumn: "ChartTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpcCalculationResults_PartProcessCharacteristics_PartProcessCharacteristicId",
                        column: x => x.PartProcessCharacteristicId,
                        principalTable: "PartProcessCharacteristics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpcCalculationResults_UploadBatches_UploadBatchId",
                        column: x => x.UploadBatchId,
                        principalTable: "UploadBatches",
                        principalColumn: "UploadBatchId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VariableMeasurements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UploadBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartId = table.Column<int>(type: "int", nullable: false),
                    ProcessId = table.Column<int>(type: "int", nullable: false),
                    MachineId = table.Column<int>(type: "int", nullable: false),
                    CharacteristicId = table.Column<int>(type: "int", nullable: false),
                    PartProcessCharacteristicId = table.Column<int>(type: "int", nullable: false),
                    LotNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SerialNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SampleNo = table.Column<int>(type: "int", nullable: false),
                    MeasuredValue = table.Column<double>(type: "float", nullable: false),
                    MeasuredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Operator = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariableMeasurements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VariableMeasurements_PartProcessCharacteristics_PartProcessCharacteristicId",
                        column: x => x.PartProcessCharacteristicId,
                        principalTable: "PartProcessCharacteristics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VariableMeasurements_UploadBatches_UploadBatchId",
                        column: x => x.UploadBatchId,
                        principalTable: "UploadBatches",
                        principalColumn: "UploadBatchId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttributeMeasurements_PartId_ProcessId_CharacteristicId_MeasuredAt",
                table: "AttributeMeasurements",
                columns: new[] { "PartId", "ProcessId", "CharacteristicId", "MeasuredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AttributeMeasurements_PartProcessCharacteristicId",
                table: "AttributeMeasurements",
                column: "PartProcessCharacteristicId");

            migrationBuilder.CreateIndex(
                name: "IX_AttributeMeasurements_UploadBatchId",
                table: "AttributeMeasurements",
                column: "UploadBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlChartCategories_ChartGroupId_CategoryCode",
                table: "ControlChartCategories",
                columns: new[] { "ChartGroupId", "CategoryCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ControlChartGroups_GroupCode",
                table: "ControlChartGroups",
                column: "GroupCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ControlChartTypes_ChartCategoryId",
                table: "ControlChartTypes",
                column: "ChartCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlChartTypes_ChartTypeCode",
                table: "ControlChartTypes",
                column: "ChartTypeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormulaDefinitions_FormulaCode",
                table: "FormulaDefinitions",
                column: "FormulaCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InspectionItems_ItemCode",
                table: "InspectionItems",
                column: "ItemCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Machines_MachineCode",
                table: "Machines",
                column: "MachineCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Machines_ProcessId",
                table: "Machines",
                column: "ProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementBatches_LotNo",
                table: "MeasurementBatches",
                column: "LotNo");

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementBatches_SerialNo",
                table: "MeasurementBatches",
                column: "SerialNo");

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementBatches_WorkOrderId",
                table: "MeasurementBatches",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementValues_BatchId",
                table: "MeasurementValues",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PartProcessCharacteristics_CharacteristicId",
                table: "PartProcessCharacteristics",
                column: "CharacteristicId");

            migrationBuilder.CreateIndex(
                name: "IX_PartProcessCharacteristics_ChartTypeId",
                table: "PartProcessCharacteristics",
                column: "ChartTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PartProcessCharacteristics_PartId_ProcessId_CharacteristicId",
                table: "PartProcessCharacteristics",
                columns: new[] { "PartId", "ProcessId", "CharacteristicId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartProcessCharacteristics_ProcessId",
                table: "PartProcessCharacteristics",
                column: "ProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_PartProcessCharacteristics_RuleGroupId",
                table: "PartProcessCharacteristics",
                column: "RuleGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Parts_PartNo",
                table: "Parts",
                column: "PartNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Processes_ProcessCode",
                table: "Processes",
                column: "ProcessCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductCode",
                table: "Products",
                column: "ProductCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductStationItems_ProductId_StationId_InspectionItemId",
                table: "ProductStationItems",
                columns: new[] { "ProductId", "StationId", "InspectionItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QualityCharacteristics_CharacteristicCode",
                table: "QualityCharacteristics",
                column: "CharacteristicCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QualityCharacteristics_DefaultChartTypeId",
                table: "QualityCharacteristics",
                column: "DefaultChartTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SpcCalculationResults_ChartTypeId",
                table: "SpcCalculationResults",
                column: "ChartTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SpcCalculationResults_PartProcessCharacteristicId_CalculatedAt",
                table: "SpcCalculationResults",
                columns: new[] { "PartProcessCharacteristicId", "CalculatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SpcCalculationResults_UploadBatchId",
                table: "SpcCalculationResults",
                column: "UploadBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_SpcRuleGroups_RuleGroupCode",
                table: "SpcRuleGroups",
                column: "RuleGroupCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpcRules_RuleGroupId_RuleCode",
                table: "SpcRules",
                columns: new[] { "RuleGroupId", "RuleCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StationOperationSessions_LotNo",
                table: "StationOperationSessions",
                column: "LotNo");

            migrationBuilder.CreateIndex(
                name: "IX_StationOperationSessions_SerialNo",
                table: "StationOperationSessions",
                column: "SerialNo");

            migrationBuilder.CreateIndex(
                name: "IX_StationOperationSessions_WorkOrderId",
                table: "StationOperationSessions",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Stations_StationCode",
                table: "Stations",
                column: "StationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UploadDetails_UploadBatchId_RowNo",
                table: "UploadDetails",
                columns: new[] { "UploadBatchId", "RowNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UploadErrors_UploadBatchId",
                table: "UploadErrors",
                column: "UploadBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadErrors_UploadDetailId",
                table: "UploadErrors",
                column: "UploadDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_VariableMeasurements_PartId_ProcessId_CharacteristicId_MeasuredAt",
                table: "VariableMeasurements",
                columns: new[] { "PartId", "ProcessId", "CharacteristicId", "MeasuredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VariableMeasurements_PartProcessCharacteristicId",
                table: "VariableMeasurements",
                column: "PartProcessCharacteristicId");

            migrationBuilder.CreateIndex(
                name: "IX_VariableMeasurements_UploadBatchId",
                table: "VariableMeasurements",
                column: "UploadBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_WorkOrderNo",
                table: "WorkOrders",
                column: "WorkOrderNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertEvents");

            migrationBuilder.DropTable(
                name: "AttributeMeasurements");

            migrationBuilder.DropTable(
                name: "FormulaDefinitions");

            migrationBuilder.DropTable(
                name: "InspectionItems");

            migrationBuilder.DropTable(
                name: "Machines");

            migrationBuilder.DropTable(
                name: "MeasurementValues");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "ProductStationItems");

            migrationBuilder.DropTable(
                name: "SpcCalculationResults");

            migrationBuilder.DropTable(
                name: "SpcRules");

            migrationBuilder.DropTable(
                name: "StationOperationSessions");

            migrationBuilder.DropTable(
                name: "Stations");

            migrationBuilder.DropTable(
                name: "UploadErrors");

            migrationBuilder.DropTable(
                name: "VariableMeasurements");

            migrationBuilder.DropTable(
                name: "WorkOrders");

            migrationBuilder.DropTable(
                name: "MeasurementBatches");

            migrationBuilder.DropTable(
                name: "UploadDetails");

            migrationBuilder.DropTable(
                name: "PartProcessCharacteristics");

            migrationBuilder.DropTable(
                name: "UploadBatches");

            migrationBuilder.DropTable(
                name: "Parts");

            migrationBuilder.DropTable(
                name: "Processes");

            migrationBuilder.DropTable(
                name: "QualityCharacteristics");

            migrationBuilder.DropTable(
                name: "SpcRuleGroups");

            migrationBuilder.DropTable(
                name: "ControlChartTypes");

            migrationBuilder.DropTable(
                name: "ControlChartCategories");

            migrationBuilder.DropTable(
                name: "ControlChartGroups");
        }
    }
}
