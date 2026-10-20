namespace MesSpc.Api.Migrations;

// Frozen SQL for AddChemicalFormulaTraceSchema; later protocol changes require a new migration.
internal static class ChemicalTraceSqlV1
{
    internal static readonly string[] Up = [
        """
        CREATE TRIGGER TR_ChemicalScheduleFrozen ON ChemicalFormulaScheduleRevisions AFTER UPDATE, DELETE AS
        BEGIN SET NOCOUNT ON;
          IF EXISTS(SELECT 1 FROM deleted WHERE PublishedAtUtc IS NOT NULL)
            THROW 51100, 'Published chemical schedules are immutable.', 1;
        END
        """,
        """
        CREATE TRIGGER TR_ChemicalEffectivityIntegrity ON ChemicalFormulaEffectivities AFTER INSERT, UPDATE, DELETE AS
        BEGIN SET NOCOUNT ON;
          DECLARE @lock bigint;
          SELECT @lock=Id FROM ChemicalFormulaScheduleRevisions WITH(UPDLOCK,HOLDLOCK)
            WHERE Id IN(SELECT ScheduleRevisionId FROM inserted UNION SELECT ScheduleRevisionId FROM deleted) ORDER BY Id;
          IF EXISTS(SELECT 1 FROM ChemicalFormulaScheduleRevisions r
            WHERE r.PublishedAtUtc IS NOT NULL AND r.Id IN(SELECT ScheduleRevisionId FROM inserted UNION SELECT ScheduleRevisionId FROM deleted))
            THROW 51101, 'Published chemical periods are immutable.', 1;
          IF EXISTS(SELECT 1 FROM inserted i JOIN ChemicalFormulaEffectivities e ON e.ScheduleRevisionId=i.ScheduleRevisionId AND e.Id<>i.Id
            WHERE (e.EffectiveUntilUtcExclusive IS NULL OR i.EffectiveFromUtc<e.EffectiveUntilUtcExclusive)
              AND (i.EffectiveUntilUtcExclusive IS NULL OR e.EffectiveFromUtc<i.EffectiveUntilUtcExclusive))
            THROW 51102, 'Chemical formula periods overlap.', 1;
        END
        """,
        """
        CREATE TRIGGER TR_ChemicalPolicyIntegrity ON ChemicalFormulaPolicies AFTER INSERT, UPDATE AS
        BEGIN SET NOCOUNT ON;
          IF EXISTS(SELECT 1 FROM inserted i JOIN PartProcessCharacteristics p ON p.Id=i.PartProcessCharacteristicId
            LEFT JOIN ChemicalFormulaScheduleRevisions r ON r.Id=i.ActiveScheduleRevisionId
            WHERE i.IsEnabled=1 AND (p.IsDeleted=1 OR p.IsEnabled=0 OR UPPER(p.ControlScope) NOT IN('CHEM','CHEMICAL')
              OR r.PublishedAtUtc IS NULL OR NOT EXISTS(SELECT 1 FROM ChemicalFormulaEffectivities e WHERE e.ScheduleRevisionId=r.Id)))
            THROW 51103, 'Only a published chemical schedule can be enabled.', 1;
        END
        """,
        """
        CREATE TRIGGER TR_ChemicalFormulaPinned ON ChemicalAnalysisFormulaVersions AFTER UPDATE, DELETE AS
        BEGIN SET NOCOUNT ON;
          IF EXISTS(SELECT 1 FROM deleted d JOIN ChemicalFormulaEffectivities e ON e.FormulaVersionId=d.Id)
            THROW 51104, 'Referenced chemical formula versions are immutable.', 1;
        END
        """,
        """
        CREATE TRIGGER TR_ChemicalFHistoryPinned ON ChemicalFTableVersionHistories AFTER UPDATE, DELETE AS
        BEGIN SET NOCOUNT ON;
          IF EXISTS(SELECT 1 FROM deleted d JOIN ChemicalFormulaEffectivities e ON e.FTableHistoryId=d.Id)
            THROW 51105, 'Referenced F history snapshots are immutable.', 1;
        END
        """,
        """
        CREATE TRIGGER TR_ChemicalReceiptImmutable ON ChemicalCalculationReceipts AFTER INSERT, UPDATE, DELETE AS
        BEGIN SET NOCOUNT ON;
          IF EXISTS(SELECT 1 FROM deleted) THROW 51106, 'Calculation receipts are immutable.', 1;
          IF EXISTS(SELECT 1 FROM inserted i JOIN ChemicalFormulaEffectivities e ON e.Id=i.EffectivityId
            JOIN ChemicalFormulaScheduleRevisions r ON r.Id=e.ScheduleRevisionId
            WHERE ISJSON(i.PayloadJson)<>1 OR LEN(i.InputHash)<>64 OR LEN(i.SourceKeyHash)<>64
              OR r.PublishedAtUtc IS NULL OR i.MeasuredAtUtc<e.EffectiveFromUtc
              OR (e.EffectiveUntilUtcExclusive IS NOT NULL AND i.MeasuredAtUtc>=e.EffectiveUntilUtcExclusive))
            THROW 51107, 'Invalid calculation evidence or measurement period.', 1;
        END
        """,
        """
        CREATE TRIGGER TR_ChemicalTraceIntegrity ON ChemicalCalculationTraces AFTER INSERT, UPDATE, DELETE AS
        BEGIN SET NOCOUNT ON;
          IF EXISTS(SELECT 1 FROM deleted) THROW 51108, 'Calculation traces are immutable.', 1;
          IF EXISTS(SELECT 1 FROM inserted i JOIN VariableMeasurements v ON v.Id=i.VariableMeasurementId
            JOIN ChemicalCalculationReceipts r ON r.Id=i.ReceiptId WHERE v.PartProcessCharacteristicId<>r.PartProcessCharacteristicId)
            THROW 51109, 'Trace and measurement belong to different control items.', 1;
        END
        """,
        """
        CREATE TRIGGER TR_ChemicalMeasurementGate ON VariableMeasurements AFTER INSERT, UPDATE AS
        BEGIN SET NOCOUNT ON;
          IF EXISTS(SELECT 1 FROM inserted i JOIN ChemicalFormulaPolicies p ON p.PartProcessCharacteristicId=i.PartProcessCharacteristicId
            WHERE p.IsEnabled=1 AND (i.ChemicalReceiptId IS NULL OR i.ChemicalSourceKeyHash IS NULL))
            THROW 51110, 'Enabled chemical items require prepared evidence.', 1;
          IF EXISTS(SELECT 1 FROM inserted i JOIN ChemicalCalculationReceipts r ON r.Id=i.ChemicalReceiptId
            WHERE r.PartProcessCharacteristicId<>i.PartProcessCharacteristicId OR i.ChemicalSourceKeyHash IS NULL OR r.SourceKeyHash<>i.ChemicalSourceKeyHash)
            THROW 51111, 'Measurement receipt scope or source key mismatch.', 1;
          IF EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id
            WHERE i.ChemicalReceiptId=d.ChemicalReceiptId AND i.ChemicalReceiptId IS NOT NULL
              AND EXISTS(SELECT i.MeasuredValue,i.RecheckValue,i.MeasuredAt EXCEPT SELECT d.MeasuredValue,d.RecheckValue,d.MeasuredAt))
            THROW 51115, 'Changed calculation inputs require a fresh receipt.', 1;
        END
        """,
        """
        CREATE TRIGGER TR_ChemicalJobIntegrity ON ChemicalStatisticsJobs AFTER INSERT, UPDATE, DELETE AS
        BEGIN SET NOCOUNT ON;
          IF EXISTS(SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.Id=d.Id
            WHERE i.Id IS NULL OR i.VariableMeasurementId<>d.VariableMeasurementId OR i.ReceiptId<>d.ReceiptId)
            THROW 51112, 'Statistics job identity cannot change or be deleted.', 1;
          IF EXISTS(SELECT 1 FROM inserted i JOIN VariableMeasurements v ON v.Id=i.VariableMeasurementId
            JOIN ChemicalCalculationReceipts r ON r.Id=i.ReceiptId WHERE v.PartProcessCharacteristicId<>r.PartProcessCharacteristicId)
            THROW 51113, 'Statistics job and receipt control items differ.', 1;
        END
        """,
        """
        IF DATABASE_PRINCIPAL_ID(N'SpcChemicalDirectWriter') IS NULL CREATE ROLE SpcChemicalDirectWriter;
        DENY INSERT,UPDATE,DELETE ON VariableMeasurements TO SpcChemicalDirectWriter;
        DENY INSERT,UPDATE,DELETE ON UploadBatches TO SpcChemicalDirectWriter;
        DENY INSERT,UPDATE,DELETE ON UploadDetails TO SpcChemicalDirectWriter;
        DENY INSERT,UPDATE,DELETE ON ChemicalFormulaPolicies TO SpcChemicalDirectWriter;
        DENY INSERT,UPDATE,DELETE ON ChemicalFormulaScheduleRevisions TO SpcChemicalDirectWriter;
        DENY INSERT,UPDATE,DELETE ON ChemicalFormulaEffectivities TO SpcChemicalDirectWriter;
        DENY INSERT,UPDATE,DELETE ON ChemicalCalculationReceipts TO SpcChemicalDirectWriter;
        DENY INSERT,UPDATE,DELETE ON ChemicalCalculationTraces TO SpcChemicalDirectWriter;
        DENY INSERT,UPDATE,DELETE ON ChemicalStatisticsJobs TO SpcChemicalDirectWriter;
        """
    ];

    internal static readonly string[] Down = [
        """
        IF DATABASE_PRINCIPAL_ID(N'SpcChemicalDirectWriter') IS NOT NULL
        BEGIN
          IF EXISTS(SELECT 1 FROM sys.database_role_members WHERE role_principal_id=DATABASE_PRINCIPAL_ID(N'SpcChemicalDirectWriter'))
            THROW 51114, 'Remove authorized writer memberships before isolated rollback.', 1;
          DROP ROLE SpcChemicalDirectWriter;
        END
        """,
        """
        DROP TRIGGER IF EXISTS TR_ChemicalScheduleFrozen;
        DROP TRIGGER IF EXISTS TR_ChemicalEffectivityIntegrity;
        DROP TRIGGER IF EXISTS TR_ChemicalPolicyIntegrity;
        DROP TRIGGER IF EXISTS TR_ChemicalFormulaPinned;
        DROP TRIGGER IF EXISTS TR_ChemicalFHistoryPinned;
        DROP TRIGGER IF EXISTS TR_ChemicalReceiptImmutable;
        DROP TRIGGER IF EXISTS TR_ChemicalTraceIntegrity;
        DROP TRIGGER IF EXISTS TR_ChemicalMeasurementGate;
        DROP TRIGGER IF EXISTS TR_ChemicalJobIntegrity;
        """
    ];
}
