using System;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Tag.GetSet.Test;

partial class TagSetGetFuncSource
{
    public static TheoryData<FlatArray<DbTag>, TagSetGetOut> OutputTestData
        =>
        new()
        {
            {
                default,
                default
            },
            {
                [
                    new()
                    {
                        Description = null
                    },
                    new()
                    {
                        Description = "Some text without tags"
                    }
                ],
                default
            },
            {
                [
                    new()
                    {
                        Description = "#Task1. Some first text"
                    },
                    new()
                    {
                        Description = string.Empty
                    },
                    new()
                    {
                        Description = "Some text.#Task_01#Task02, Some second # text"
                    },
                    new()
                    {
                        Description = "#SomeTag"
                    },
                    new()
                    {
                        Description = "#Task1; Another text"
                    },
                    new()
                    {
                        Description = null
                    },
                    new()
                    {
                        Description = "Text#One"
                    }
                ],
                new()
                {
                    Tags = new("#Task1", "#Task_01", "#Task02", "#SomeTag", "#One")
                }
            }
        };
}