namespace DatasetDownloader;

internal class Downloader
{
    protected readonly HttpClient _client;
    public Downloader(string base_url = "")
    {
        HttpClientHandler handler = new()
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 10,
        };
        _client = new(handler)
        {
            BaseAddress = new(base_url),
        };
        _client.DefaultRequestHeaders.Add(
            "User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");
    }
    protected async Task<List<string>> GetLinksFromUrl(string url, string pre = "<a href=", string post = ">")
    {
        List<string> datalinks = [];
        string? html = await _client.GetStringAsync(url);
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
    protected async Task DownloadSingleFile(string link, string filename)
    {
        Console.WriteLine($"Downloading {link} to {filename}");
        using var response = await _client.GetAsync(link, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        long? contentLength = response.Content.Headers.ContentLength;
        if (!contentLength.HasValue || contentLength.Value <= 0)
        {
            Console.WriteLine("Could not determine file size. Downloading without progress indication...");
            using var fStream = File.Create(filename);
            await response.Content.CopyToAsync(fStream);
            return;
        }
        using var responseStream = await response.Content.ReadAsStreamAsync();
        using var fileStream = File.Create(filename);
        var buffer = new byte[8192];
        long totalBytesRead = 0;
        int bytesRead;
        int lastProgress = -1;
        DateTime lastUpdateTime = DateTime.UtcNow;
        long lastUpdateBytes = 0;
        while ((bytesRead = await responseStream.ReadAsync(buffer.AsMemory(0, buffer.Length), CancellationToken.None)) != 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), CancellationToken.None);
            totalBytesRead += bytesRead;
            int progress = (int)((double)totalBytesRead / contentLength.Value * 100);
            if (progress != lastProgress)
            {
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
}