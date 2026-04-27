using GarageGroup.Infra;
using Microsoft.AspNetCore.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class IsSuccessMiddleware
{
    internal static EndpointApplication UseIsSuccessMiddleware(this EndpointApplication builder)
    {
        builder.Use(AddIsSuccessFieldInResponseBodyAsync);
        ((ISwaggerBuilder)builder).Use(InnerConfigureSwagger);

        return builder;
    }
}