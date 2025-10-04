using System;
using System.Diagnostics;

namespace IssueAgent.Agent.Instrumentation;

public class StartupMetricsRecorder
{
    public virtual IDisposable BeginMeasurement()
    {
        var stopwatch = Stopwatch.StartNew();
        return new MeasurementScope(stopwatch, this);
    }

    protected virtual void Record(TimeSpan duration)
    {
        // Intentionally left blank for derived classes or logging decorators.
    }

    private sealed class MeasurementScope : IDisposable
    {
        private readonly Stopwatch _stopwatch;
        private readonly StartupMetricsRecorder _recorder;
        private bool _disposed;

        public MeasurementScope(Stopwatch stopwatch, StartupMetricsRecorder recorder)
        {
            _stopwatch = stopwatch;
            _recorder = recorder;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _stopwatch.Stop();
            _recorder.Record(_stopwatch.Elapsed);
        }
    }
}
