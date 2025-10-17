using GarageGroup.Infra;
using System;
using System.Text;
using System.Text.Json.Serialization;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class LeadJson : IProjectJson, IProjectDataverseInputBuilder
{
    private const string EntityPluralName = "leads";

    private const string FieldCompanyName = "companyname";

    private const string FieldSubjectName = "subject";

    public static DataverseEntityGetIn BuildDataverseEntityGetIn(Guid leadId, Guid callerObjectId)
        =>
        new(
            entityPluralName: EntityPluralName,
            entityKey: new DataversePrimaryKey(leadId),
            selectFields: [FieldCompanyName, FieldSubjectName])
        {
            CallerObjectId = callerObjectId
        };

    [JsonPropertyName("leadid")]
    public Guid Id { get; init; }

    [JsonPropertyName(FieldCompanyName)]
    public string? CompanyName { get; init; }

    [JsonPropertyName(FieldSubjectName)]
    public string? Subject { get; init; }

    string? IProjectJson.Name
    {
        get
        {
            if (string.IsNullOrEmpty(CompanyName))
            {
                return Subject;
            }

            var builder = new StringBuilder(Subject);
            if (string.IsNullOrEmpty(Subject) is false)
            {
                builder = builder.Append(' ');
            }

            return builder.Append('(').Append(CompanyName).Append(')').ToString();
        }
    }

    string IProjectJson.LookupValue
        =>
        $"/{EntityPluralName}({Id:D})";

    string IProjectJson.LookupEntity { get; }
        =
        "lead";
}
