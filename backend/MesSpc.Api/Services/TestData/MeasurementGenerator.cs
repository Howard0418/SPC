using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Domain.Enums;

namespace MesSpc.Api.Services.TestData;

public class MeasurementGenerator(SpcSampleGenerator spc)
{
    public (List<MeasurementBatch> batches, int lots, int serials) CreateBatches(
        List<WorkOrder> workOrders,
        List<Station> stations,
        List<InspectionItem> items,
        GenerateTestDataRequest req,
        string runId)
    {
        var batches = new List<MeasurementBatch>();
        var lotCount = 0;
        var serialCount = 0;
        var lotSerial = 1;
        var snSerial = 1;

        foreach (var wo in workOrders)
        {
            for (var l = 0; l < req.LotPerWorkOrder; l++)
            {
                lotCount++;
                var lotNo = $"{TestDataTags.TestPrefix}LOT_{runId}_{lotSerial:0000}";
                lotSerial++;
                for (var m = 0; m < Math.Max(1, req.MeasurementPerLot / 20); m++)
                {
                    var station = stations[(l + m) % stations.Count];
                    var serialNo = $"{TestDataTags.TestPrefix}SN_{runId}_{snSerial:0000}";
                    snSerial++;
                    serialCount++;
                    var batch = new MeasurementBatch
                    {
                        BatchNo = $"{TestDataTags.TestPrefix}BATCH_{runId}_{wo.Id}_{l + 1}_{m + 1}",
                        ProductId = wo.ProductId,
                        StationId = station.Id,
                        WorkOrderId = wo.Id,
                        LotNo = lotNo,
                        SerialNo = serialNo,
                        MeasuredAt = DateTime.UtcNow.AddMinutes(-(l * 10 + m)),
                        OperatorName = $"{TestDataTags.E2ePrefix}GEN",
                        SourceType = SourceType.Manual
                    };

                    var samplePerItem = Math.Max(10, req.MeasurementPerLot / Math.Max(1, items.Count));
                    foreach (var item in items)
                    {
                        var sigma = Math.Max(0.01, (item.Usl ?? item.TargetValue ?? 1) - (item.Lsl ?? item.TargetValue ?? 0)) / 12.0;
                        var mean = item.TargetValue ?? ((item.Usl ?? 0) + (item.Lsl ?? 0)) / 2.0;
                        var values = spc.GenerateSeries(samplePerItem, mean, sigma, item.Lsl, item.Usl, item.Lcl, item.Ucl, req.GenerateSpcAnomalies);
                        var sampleNo = 1;
                        foreach (var v in values)
                        {
                            batch.Values.Add(new MeasurementValue
                            {
                                InspectionItemId = item.Id,
                                SampleNo = sampleNo++,
                                ValueNumeric = Math.Round(v, 4)
                            });
                        }
                    }
                    batches.Add(batch);
                }
            }
        }

        return (batches, lotCount, serialCount);
    }
}

