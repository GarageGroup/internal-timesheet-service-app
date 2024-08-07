﻿using GarageGroup.Infra;
using PrimeFuncPack;
using System;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    [EndpointApplicationExtension]
    internal static Dependency<UserSignOutEndpoint> UseUserSignOutEndpoint()
        =>
        Pipeline.Pipe(
            UseDataverseApi())
        .With(
            UseBotApi())
        .UseUserSignOutEndpoint();
}