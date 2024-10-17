public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureLogging((context, logging) =>
            {
                // EventLog logger'ı ekliyoruz
                logging.AddEventLog(eventLogSettings =>
                {
                    eventLogSettings.SourceName = "DiskCleanupService"; // Event Viewer'da gözükecek kaynak ismi
                });
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<DiskCleanupWorker>();
            });
}