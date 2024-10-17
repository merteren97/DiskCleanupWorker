public class DiskCleanupWorker : BackgroundService
{
    private readonly ILogger<DiskCleanupWorker> _logger;
    private readonly string _driveLetter = "C:";  // Disk sürücüsü harfi
    private readonly double _maxPercentage = 60.0;  // Maksimum doluluk yüzdesi
    private readonly double _minPercentage = 40.0;  // Minimum doluluk yüzdesi
    private readonly string _pathForImages = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Visiomex", "Images");
    private readonly string _pathForResultImages = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Visiomex", "ResultImage");
    public DiskCleanupWorker(ILogger<DiskCleanupWorker> logger)
    {
        _logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DiskCleanupWorker started at: {time}", DateTimeOffset.Now);

        try
        {
            // Disk Temizleyici fonksiyonu çağır
            await DiskCleaner();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hata oluştu.");
        }

        _logger.LogInformation("DiskCleanupWorker finished at: {time}", DateTimeOffset.Now);
    }
    public async Task DiskCleaner()
    {
        double diskUsagePercentage = GetDiskUsagePercentage(_driveLetter);

        _logger.LogInformation($"[{DateTime.Now}] Disk Doluluğu: {diskUsagePercentage}%");

        if (diskUsagePercentage >= _maxPercentage)
        {
            DeleteOldestFolders(_pathForImages);
        }
    }
    private double GetDiskUsagePercentage(string driveLetter)
    {
        DriveInfo drive = new DriveInfo(driveLetter);

        if (drive.IsReady)
        {
            long totalSpace = drive.TotalSize;
            long freeSpace = drive.AvailableFreeSpace;
            double usedSpace = totalSpace - freeSpace;
            return (usedSpace / totalSpace) * 100;
        }
        else
        {
            _logger.LogError("Disk hazır değil.");
            return 0;
        }
    }
    private void DeleteOldestFolders(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            var directories = Directory.GetDirectories(directoryPath)
                                       .Select(d => new DirectoryInfo(d))
                                       .OrderBy(d => d.LastWriteTime)
                                       .ToList();

            foreach (var directory in directories)
            {
                try
                {
                    Directory.Delete(directory.FullName, true);
                    _logger.LogInformation($"Silindi: {directory.FullName}");

                    double currentDiskUsage = GetDiskUsagePercentage(_driveLetter);
                    _logger.LogInformation($"Disk Doluluğu: {currentDiskUsage}");

                    if (currentDiskUsage <= _minPercentage)
                    {
                        _logger.LogInformation($"Disk doluluğu {_minPercentage}% seviyesine indi. Silme işlemi durduruluyor.");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Klasör silme hatası.");
                }
            }
        }

        if (GetDiskUsagePercentage(_driveLetter) > _minPercentage)
        {
            _logger.LogInformation("Klasörler silindi ama minimum disk doluluğuna ulaşılamadı. Dosyalar siliniyor...");
            DeleteOldestFiles(_pathForResultImages);
        }
    }
    private void DeleteOldestFiles(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            var files = Directory.GetFiles(directoryPath)
                                 .Select(f => new FileInfo(f))
                                 .OrderBy(f => f.LastWriteTime)
                                 .ToList();

            foreach (var file in files)
            {
                try
                {
                    File.Delete(file.FullName);
                    _logger.LogInformation($"Silindi: {file.FullName}");

                    double currentDiskUsage = GetDiskUsagePercentage(_driveLetter);
                    _logger.LogInformation($"Disk Doluluğu: {currentDiskUsage}");

                    if (currentDiskUsage <= _minPercentage)
                    {
                        _logger.LogInformation($"Disk doluluğu {_minPercentage}% seviyesine indi. Silme işlemi durduruluyor.");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Dosya silme hatası.");
                }
            }
        }
        else
        {
            _logger.LogError($"Dizin bulunamadı: {directoryPath}");
        }
    }
}