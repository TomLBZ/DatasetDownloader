namespace DatasetDownloader;

internal class Downloader
{
    protected readonly HttpClient _client;
    protected readonly Timer _timer;
    protected bool _isSpeedUpdatable = false;
    protected readonly string _storePath;
    protected readonly string _linkFile;
    public Downloader(string baseUrl, string storePath, string linkFile)
    {
        HttpClientHandler handler = new()
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 10,
        };
        _client = new(handler);
        if (!string.IsNullOrEmpty(baseUrl)) _client.BaseAddress = new Uri(baseUrl);
        _client.DefaultRequestHeaders.Add(
            "User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3"
        );
        _timer = new Timer((_) => _isSpeedUpdatable = true, null, 1000, 1000);
        _storePath = storePath;
        _linkFile = linkFile;
    }
    protected List<string> GetLinksFromUrl(string url, string pre = "<a href=", string post = ">")
    {
        List<string> datalinks = [];
        string html = _client.GetStringAsync(url).Result;
        if (string.IsNullOrEmpty(html)) return datalinks;
        foreach (string line in html.Split('\n'))
        {
            if (line.Contains(pre))
            {
                string link = line.Split(pre)[1].Split(post)[0];
                if (link.Contains('"')) link = link.Split('"')[1];
                if (!string.IsNullOrEmpty(link) && char.IsLetterOrDigit(link[0])) datalinks.Add(url + link);
            }
        }
        return datalinks;
    }
    protected void DownloadSingleFile(string link, string filename)
    {
        Console.WriteLine($"Downloading {link} to {filename}");
        using var response = _client.GetAsync(link, HttpCompletionOption.ResponseHeadersRead).Result;
        response.EnsureSuccessStatusCode();
        long? contentLength = response.Content.Headers.ContentLength;
        if (!contentLength.HasValue || contentLength.Value <= 0)
        {
            Console.WriteLine("Could not determine file size. Downloading without progress indication...");
            using var fStream = File.Create(filename);
            response.Content.CopyToAsync(fStream).GetAwaiter().GetResult();
            return;
        }
        using var responseStream = response.Content.ReadAsStreamAsync().Result;
        using var fileStream = File.Create(filename);
        var buffer = new byte[8192];
        long totalBytesRead = 0;
        int bytesRead;
        int lastProgress = -1;
        DateTime lastUpdateTime = DateTime.UtcNow;
        long lastUpdateBytes = 0;
        while ((bytesRead = responseStream.ReadAsync(buffer.AsMemory(0, buffer.Length), CancellationToken.None).Result) != 0)
        {
            fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), CancellationToken.None).GetAwaiter().GetResult();
            totalBytesRead += bytesRead;
            int progress = (int)((double)totalBytesRead / contentLength.Value * 100);
            if (_isSpeedUpdatable || progress == 100)
            {
                _isSpeedUpdatable = false;
                var now = DateTime.UtcNow;
                double elapsedSeconds = (now - lastUpdateTime).TotalSeconds;
                if (elapsedSeconds > 0)
                {
                    long bytesSinceLastUpdate = totalBytesRead - lastUpdateBytes;
                    double speedBps = bytesSinceLastUpdate / elapsedSeconds;
                    double speedKiBps = speedBps / 1024.0;
                    if (speedKiBps >= 1024)
                    {
                        double speedMiBps = speedKiBps / 1024.0;
                        Console.Write($"\rProgress: {progress}% @ {speedMiBps:0.0} MiB/s");
                    }
                    else
                    {
                        Console.Write($"\rProgress: {progress}% @ {speedKiBps:0.0} KiB/s");
                    }
                    lastUpdateTime = now;
                    lastUpdateBytes = totalBytesRead;
                }
                lastProgress = progress;
            }
        }
        Console.WriteLine();
    }
    public virtual void Download()
    {
        throw new NotImplementedException();
    }
}