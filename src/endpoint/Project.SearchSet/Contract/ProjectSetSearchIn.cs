using System;
using System.Diagnostics.CodeAnalysis;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

using static ProjectSetSearchMetadata;

public sealed record class ProjectSetSearchIn
{
    public ProjectSetSearchIn(
        [ClaimIn("oid")] Guid systemUserId,
        [JsonBodyIn, SwaggerDescription(In.SearchTextDescription), StringExample(In.SearchTextExample)] [AllowNull] string searchText,
        [JsonBodyIn, SwaggerDescription(In.TopDescription), IntegerExample(In.TopExample)] int? top)
    {
        SystemUserId = systemUserId;
        SearchText = searchText.OrEmpty();
        Top = top;
    }

    public Guid SystemUserId { get; }

    public string SearchText { get; }

    public int? Top { get; }
}