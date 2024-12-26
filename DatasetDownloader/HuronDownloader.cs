namespace DatasetDownloader;

internal class HuronDownloader(string baseUrl, string storePath, string linkFile) : Downloader(baseUrl, storePath, linkFile)
{
    private List<string> GetHuronPageLinks(string pre = "<a href=", string post = ">", string filename = "huron_links.txt")
    {
        if (File.Exists(filename))
        {
            Console.WriteLine($"File {filename} already exists. Skipping.");
            return [.. File.ReadAllLines(filename)];
        }
        Console.WriteLine($"Downloading from {_client.BaseAddress}");
        List<string> datalinks = GetLinksFromUrl("", pre, post);
        List<string>[] links = datalinks.Select(link => GetLinksFromUrl(link, pre, post)).ToArray();
        datalinks = links.Aggregate((a, b) => [.. a, .. b]);
        File.WriteAllLines(filename, datalinks);
        return datalinks;
    }
    private void DownloadHuron(List<string> links, string store_path)
    {
        int index = 0;
        foreach (var link in links)
        {
            Console.WriteLine($"Downloading {++index}/{links.Count}: {link}");
            string filename = Path.Combine(store_path, link);
            if (File.Exists(filename))
            {
                Console.WriteLine($"File {filename} already exists. Skipping.");
                continue;
            }
            string? path_stud = Path.GetDirectoryName(filename);
            if (string.IsNullOrEmpty(path_stud)) continue;
            if (!Directory.Exists(path_stud)) Directory.CreateDirectory(path_stud);
            DownloadSingleFile(link, filename);
        }
    }
    public override void Download()
    {
        List<string> links = GetHuronPageLinks(pre: "<a href=", post: ">", filename: _linkFile);
        DownloadHuron(links, _storePath);
    }
}