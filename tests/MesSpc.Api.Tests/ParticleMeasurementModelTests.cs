using FluentAssertions;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace MesSpc.Api.Tests;

public class ParticleMeasurementModelTests
{
    [Fact]
    public void ParticleMeasurement_ShouldProvideLongFormatSchemaAndIndexes()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new AppDbContext(options);
        var entity = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(ParticleMeasurement));

        entity.Should().NotBeNull();
        ColumnType(entity, nameof(ParticleMeasurement.ParticleSize)).Should().Be("decimal(6,3)");
        ColumnType(entity, nameof(ParticleMeasurement.SamplingVolume)).Should().Be("decimal(18,6)");
        entity.FindProperty(nameof(ParticleMeasurement.SamplingVolumeUnit))!.GetMaxLength().Should().Be(32);
        ColumnType(entity, nameof(ParticleMeasurement.SamplingDurationSeconds)).Should().Be("decimal(18,3)");

        var indexes = entity.GetIndexes().ToList();
        indexes.Should().Contain(index => HasProperties(index,
            nameof(ParticleMeasurement.Location), nameof(ParticleMeasurement.ParticleSize), nameof(ParticleMeasurement.MeasurementTime)));
        indexes.Should().Contain(index => HasProperties(index,
            nameof(ParticleMeasurement.ParticleSize), nameof(ParticleMeasurement.MeasurementTime), nameof(ParticleMeasurement.Location)));
        indexes.Should().Contain(index => HasProperties(index, nameof(ParticleMeasurement.UploadBatchId)));
        indexes.Should().Contain(index => index.IsUnique && HasProperties(index,
            nameof(ParticleMeasurement.UploadBatchId), nameof(ParticleMeasurement.SourceSheet),
            nameof(ParticleMeasurement.SourceRow), nameof(ParticleMeasurement.SourceColumn)));
        entity.GetCheckConstraints().Should().Contain(constraint =>
            constraint.Name == "CK_ParticleMeasurements_Count_NonNegative" && constraint.Sql == "[Count] >= 0");

        var uploadBatchForeignKey = entity.GetForeignKeys().Single(foreignKey =>
            foreignKey.Properties.Select(property => property.Name).SequenceEqual([nameof(ParticleMeasurement.UploadBatchId)]));
        uploadBatchForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
    }

    private static bool HasProperties(IIndex index, params string[] names) =>
        index.Properties.Select(property => property.Name).SequenceEqual(names);

    private static string? ColumnType(IEntityType entity, string propertyName) =>
        entity.FindProperty(propertyName)?.FindAnnotation(RelationalAnnotationNames.ColumnType)?.Value as string;
}
