﻿using GarageGroup.Infra;
using System;

namespace GarageGroup.Internal.Timesheet;

partial record class DbLastProject
{
    [DbSelect(All, AliasName, $"{AliasName}.regardingobjectid", GroupBy = true)]
    public Guid ProjectId { get; init; }

    [DbSelect(All, AliasName, ProjectTypeCodeFieldName, GroupBy = true)]
    public int ProjectTypeCode { get; init; }

    [DbSelect(All, AliasName, $"MAX({AliasName}.regardingobjectidname)")]
    public string? ProjectName { get; init; }

    [DbSelect(All, ProjectAlias, $"MAX({ProjectAlias}.gg_comment)")]
    public string? ProjectComment { get; init; }

    [DbSelect(All, AliasName, SelectLastSubjectSql)]
    public string? Subject { get; init; }

    [DbSelect(All, AliasName, $"MAX({AliasName}.gg_date)")]
    private DateTime MaxDate { get; init; }

    [DbSelect(All, AliasName, $"MAX({AliasName}.createdon)")]
    private DateTime MaxCreatedOn { get; init; }
}