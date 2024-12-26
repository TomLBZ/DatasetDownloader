using System.Text.Json;

namespace DatasetDownloader;
internal struct ScandDataObject
{
    public string name;
    public long byte_size;
    public string url;
    public ScandDataObject(string name, long byte_size, string url)
    {
        this.name = name;
        this.byte_size = byte_size;
        this.url = url;
    }
    public static List<ScandDataObject> Load(string filename)
    {
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(filename));
        JsonElement root = doc.RootElement;
        JsonElement distribution = root.GetProperty("distribution"); // is a list of objects
        List<ScandDataObject> dataObjects = [];
        foreach (JsonElement obj in distribution.EnumerateArray())
        {
            string? name = obj.GetProperty("name").GetString();
            long byte_size = obj.GetProperty("contentSize").GetInt64();
            string? url = obj.GetProperty("contentUrl").GetString();
            if (name == null || url == null) continue;
            dataObjects.Add(new ScandDataObject(name, byte_size, url));
        }
        return dataObjects;
    }
}
internal class ScandDownloader : Downloader
{
    public async Task DownloadScand(List<ScandDataObject> dataObjects, string store_path)
    {
        int index = 0;
        foreach (var dataObject in dataObjects)
        {
            Console.WriteLine($"Downloading {++index}/{dataObjects.Count}: {dataObject.name}");
            string filename = Path.Combine(store_path, dataObject.name);
            if (File.Exists(filename))
            {
                Console.WriteLine($"File {filename} already exists. Skipping.");
                continue;
            }
            string? path_stud = Path.GetDirectoryName(filename);
            if (string.IsNullOrEmpty(path_stud)) continue;
            if (!Directory.Exists(path_stud)) Directory.CreateDirectory(path_stud);
            await DownloadSingleFile(dataObject.url, filename);
        }
    }
}
