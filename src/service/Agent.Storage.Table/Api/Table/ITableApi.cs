using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;

namespace GarageGroup.Internal.Timesheet;

internal interface ITableApi
{
    ValueTask<TableEntity?> GetAsync(string partitionKey, string rowKey, CancellationToken cancellationToken);

    ValueTask AddAsync(TableEntity entity, CancellationToken cancellationToken);

    ValueTask UpdateAsync(TableEntity entity, ETag etag, CancellationToken cancellationToken);
}
