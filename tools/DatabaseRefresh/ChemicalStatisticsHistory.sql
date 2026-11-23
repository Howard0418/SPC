SELECT Id, VariableMeasurementId, ReceiptId, Status, Attempts, CreatedAtUtc,
       CASE WHEN Status = N'Completed' THEN NextAttemptAtUtc
            ELSE CONVERT(datetime2(7), '9999-12-31T23:59:59.9999999', 126) END AS NextAttemptAtUtc,
       LastError
FROM {{StatisticsTable}};
