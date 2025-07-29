namespace InnoHub.ModelDTO
{
    public class PerformanceMonitoringConfig
    {
        public bool EnableMetrics { get; set; } = true;
        public bool LogPredictions { get; set; } = true;
        public bool MonitorAccuracy { get; set; } = true;
        public bool AlertOnLowAccuracy { get; set; } = true;
    }
}
