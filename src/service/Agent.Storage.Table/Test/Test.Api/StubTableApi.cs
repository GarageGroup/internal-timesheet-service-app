using System;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Storage.Table.Test;

internal sealed class StubTableApi : ITableApi
{
    public Func<string, string, CancellationToken, ValueTask<TableEntity?>> GetAsyncStub { get; init; }
        = static (_, _, _) => ValueTask.FromResult<TableEntity?>(null);

    public Func<TableEntity, CancellationToken, ValueTask> AddAsyncStub { get; init; }
        = static (_, _) => ValueTask.CompletedTask;

    public Func<TableEntity, ETag, CancellationToken, ValueTask> UpdateAsyncStub { get; init; }
        = static (_, _, _) => ValueTask.CompletedTask;

    public ValueTask<TableEntity?> GetAsync(string partitionKey, string rowKey, CancellationToken cancellationToken)
        =>
        GetAsyncStub.Invoke(partitionKey, rowKey, cancellationToken);

    public ValueTask AddAsync(TableEntity entity, CancellationToken cancellationToken)
        =>
        AddAsyncStub.Invoke(entity, cancellationToken);

    public ValueTask UpdateAsync(TableEntity entity, ETag etag, CancellationToken cancellationToken)
        =>
        UpdateAsyncStub.Invoke(entity, etag, cancellationToken);
}
