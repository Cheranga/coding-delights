using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace OrderProcessorFuncApp.Core;

internal sealed class CustomTelemetryInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.Cloud.RoleName = "OrderProcessorFuncApp";
        telemetry.Context.Cloud.RoleInstance = Environment.MachineName;
    }
}
