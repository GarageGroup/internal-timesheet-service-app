﻿using GarageGroup.Infra;
using System.Text.RegularExpressions;

namespace GarageGroup.Internal.Timesheet;

internal sealed partial class TagSetGetFunc(ISqlQueryEntitySetSupplier sqlApi) : ITagSetGetFunc
{
    private const string TagStartSymbol = "#";

    private static readonly Regex TagRegex;

    private static readonly IDbFilter DescriptionTagFilter;

    static TagSetGetFunc()
    {
        TagRegex = CreateTagRegex();
        DescriptionTagFilter = DbTag.BuildDescriptionFilter(TagStartSymbol);
    }

    [GeneratedRegex($"{TagStartSymbol}\\w+", RegexOptions.CultureInvariant)]
    private static partial Regex CreateTagRegex();
}