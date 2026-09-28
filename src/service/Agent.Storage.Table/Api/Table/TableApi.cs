using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;

namespace GarageGroup.Internal.Timesheet;

internal sealed class TableApi(TableClient tableClient) : ITableApi
{
    public async ValueTask<TableEntity?> GetAsync(
        string partitionKey,
        string rowKey,
        CancellationToken cancellationToken)
    {
        var response = await tableClient.GetEntityIfExistsAsync<TableEntity>(
            partitionKey,
            rowKey,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return response.HasValue ? response.Value : null;
    }

    public async ValueTask AddAsync(TableEntity entity, CancellationToken cancellationToken)
        =>
        _ = await tableClient.AddEntityAsync(entity, cancellationToken).ConfigureAwait(false);

    public async ValueTask UpdateAsync(TableEntity entity, ETag etag, CancellationToken cancellationToken)
        =>
        _ = await tableClient.UpdateEntityAsync(entity, etag, TableUpdateMode.Replace, cancellationToken).ConfigureAwait(false);
}
