using Microsoft.AspNetCore.Components.Server.Circuits;

namespace FiveThreeOneTracker.Services;

/// <summary>
/// Logs Blazor Server circuit lifecycle events (open/close/connection up-down) so that
/// mobile-only circuit failures ("An unhandled error has occurred") can be correlated
/// with server-side exceptions in the logs.
/// </summary>
public class LoggingCircuitHandler : CircuitHandler
{
    private readonly ILogger<LoggingCircuitHandler> _logger;

    public LoggingCircuitHandler(ILogger<LoggingCircuitHandler> logger)
    {
        _logger = logger;
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[CIRCUIT] Opened. CircuitId={CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[CIRCUIT] Connection up. CircuitId={CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogWarning("[CIRCUIT] Connection down. CircuitId={CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[CIRCUIT] Closed. CircuitId={CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }
}
