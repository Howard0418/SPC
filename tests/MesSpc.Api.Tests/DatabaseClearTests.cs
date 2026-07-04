using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services.TestData;
using MesSpc.Api.Domain.Entities;
using FluentAssertions;

namespace MesSpc.Api.Tests;

public class DatabaseClearTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;

    public DatabaseClearTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ClearAllDatabaseDataAsync_Should_Wipe_All_Tables()
    {
        // Arrange
        // Add some mock master and transactional data
        var product = new Product { ProductCode = "P-9999", ProductName = "Test Product" };
        var station = new Station { StationCode = "ST-99", StationName = "Test Station" };
        var item = new InspectionItem { ItemCode = "ITEM-99", ItemName = "Test Item" };
        _context.Products.Add(product);
        _context.Stations.Add(station);
        _context.InspectionItems.Add(item);
        await _context.SaveChangesAsync();

        var psi = new ProductStationItem
        {
            ProductId = product.Id,
            StationId = station.Id,
            InspectionItemId = item.Id,
            SampleSize = 5,
            IsActive = true
        };
        _context.ProductStationItems.Add(psi);

        var workOrder = new WorkOrder { WorkOrderNo = "WO-9999", ProductId = product.Id, PlannedQty = 100 };
        _context.WorkOrders.Add(workOrder);
        await _context.SaveChangesAsync();

        var batch = new MeasurementBatch
        {
            BatchNo = "B-9999",
            ProductId = product.Id,
            StationId = station.Id,
            WorkOrderId = workOrder.Id,
            LotNo = "L-9999",
            MeasuredAt = DateTime.UtcNow
        };
        _context.MeasurementBatches.Add(batch);
        await _context.SaveChangesAsync();

        var value = new MeasurementValue
        {
            BatchId = batch.Id,
            InspectionItemId = item.Id,
            SampleNo = 1,
            ValueNumeric = 12.34
        };
        _context.MeasurementValues.Add(value);

        var alert = new AlertEvent
        {
            PartId = product.Id,
            ProcessId = station.Id,
            CharacteristicId = item.Id,
            ActualValue = 12.34,
            Message = "Test alert"
        };
        _context.AlertEvents.Add(alert);

        var lot = new LotMaster
        {
            LotNo = "L-9999",
            WorkOrderId = workOrder.Id,
            PartId = product.Id,
            CurrentQty = 100
        };
        _context.LotMasters.Add(lot);
        await _context.SaveChangesAsync();

        // Add a child lot to test self-reference logic
        var childLot = new LotMaster
        {
            LotNo = "L-9999-A",
            ParentLotId = lot.Id,
            WorkOrderId = workOrder.Id,
            PartId = product.Id,
            CurrentQty = 50
        };
        _context.LotMasters.Add(childLot);
        await _context.SaveChangesAsync();

        // Ensure everything is saved and exists in DB
        (await _context.Products.CountAsync()).Should().Be(1);
        (await _context.LotMasters.CountAsync()).Should().Be(2);
        (await _context.MeasurementValues.CountAsync()).Should().Be(1);
        (await _context.AlertEvents.CountAsync()).Should().Be(1);

        // Act
        var seeder = new DatabaseSeeder(_context);
        var response = await seeder.ClearAllDatabaseDataAsync();

        // Assert
        response.Products.Should().Be(1);
        response.LotMasters.Should().Be(2);
        response.MeasurementValues.Should().Be(1);
        response.AlertEvents.Should().Be(1);

        // Verify database is completely empty
        (await _context.Products.AnyAsync()).Should().BeFalse();
        (await _context.Stations.AnyAsync()).Should().BeFalse();
        (await _context.InspectionItems.AnyAsync()).Should().BeFalse();
        (await _context.ProductStationItems.AnyAsync()).Should().BeFalse();
        (await _context.WorkOrders.AnyAsync()).Should().BeFalse();
        (await _context.MeasurementBatches.AnyAsync()).Should().BeFalse();
        (await _context.MeasurementValues.AnyAsync()).Should().BeFalse();
        (await _context.AlertEvents.AnyAsync()).Should().BeFalse();
        (await _context.LotMasters.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task ClearTaggedTestDataAsync_Should_Remove_Only_Tagged_Users_And_Keep_Master_Data()
    {
        _context.Operators.AddRange(
            new Operator { OperatorCode = "E2E-OP-001", OperatorName = "測試人員", Role = "Editor" },
            new Operator { OperatorCode = "OP-REAL-001", OperatorName = "正式人員", Role = "Viewer" });
        _context.Parts.Add(new Part { PartNo = "P-REAL-001", PartName = "正式料號" });
        await _context.SaveChangesAsync();

        var seeder = new DatabaseSeeder(_context);
        var response = await seeder.ClearTaggedTestDataAsync();

        response.Operators.Should().Be(1);
        (await _context.Operators.AnyAsync(x => x.OperatorCode == "E2E-OP-001")).Should().BeFalse();
        (await _context.Operators.AnyAsync(x => x.OperatorCode == "OP-REAL-001")).Should().BeTrue();
        (await _context.Parts.AnyAsync(x => x.PartNo == "P-REAL-001")).Should().BeTrue();
    }
}
