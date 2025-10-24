using GarageGroup.Infra;
using System;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Cost.Endpoint.CostPeriod.GetSet.Test;

internal static partial class PeriodSetGetFuncSource
{
    public static TheoryData<DataverseEntitySetGetOut<PeriodJson>, DateTime, PeriodSetGetOut> OutputTestData
        =>
        new()
        {
            {
                new(
                    value:
                    [
                        new()
                        {
                            Name = "Some first name",
                            From = new(2024, 6, 1, 12, 23, 1),
                            To = new(2024, 6, 30, 12, 22, 3)
                        },
                        new()
                        {
                            Name = "Some second name",
                            From = new(2024, 5, 10, 12, 22, 3),
                            To = new(2024, 5, 15, 12, 22, 3)
                        }
                    ]),
                new(2024, 6, 10, 12, 0, 0),
                new()
                {
                    Periods =
                    [
                        new(
                            name: "Some first name",
                            dateFrom: new(2024, 6, 1),
                            dateTo: new(2024, 6, 10)),
                        new(
                            name: "Some second name",
                            dateFrom: new(2024, 5, 10),
                            dateTo: new(2024, 5, 15))
                    ]
                }
            }
        };
}