namespace DatasetDownloader;

internal class HuronDownloader : Downloader
{
    public HuronDownloader(string base_url = "") : base(base_url) { }
    public async Task<List<string>> GetHuronPageLinksAsync(string pre = "<a href=", string post = ">", string filename = "huron_links.txt")
    {
        if (File.Exists(filename))
        {
            Console.WriteLine($"File {filename} already exists. Skipping.");
            return [.. File.ReadAllLines(filename)];
        }
        Console.WriteLine($"Downloading from {_client.BaseAddress}");
        List<string> datalinks = await GetLinksFromUrl("", pre, post);
        List<string>[] links = await Task.WhenAll(datalinks.Select(link => GetLinksFromUrl(link, pre, post)));
        datalinks = links.Aggregate((a, b) => [.. a, .. b]);
        File.WriteAllLines(filename, datalinks);
        return datalinks;
    }
    public List<string> GetHuronPageLinks(string pre = "<a href=", string post = ">", string filename = "huron_links.txt")
    {
        return GetHuronPageLinksAsync(pre, post, filename).Result;
    }
    public async Task DownloadHuron(List<string> links, string store_path)
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
            await DownloadSingleFile(link, filename);
        }
    }
}