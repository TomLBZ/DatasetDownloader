using System.Text.Json;

namespace DatasetDownloader;
internal struct DataObject
{
    public string name;
    public long byte_size;
    public string url;
    public DataObject(string name, long byte_size, string url)
    {
        this.name = name;
        this.byte_size = byte_size;
        this.url = url;
    }
    public static List<DataObject> Load(string filename)
    {
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(filename));
        JsonElement root = doc.RootElement;
        JsonElement distribution = root.GetProperty("distribution"); // is a list of objects
        List<DataObject> dataObjects = [];
        foreach (JsonElement obj in distribution.EnumerateArray())
        {
            string? name = obj.GetProperty("name").GetString();
            long byte_size = obj.GetProperty("contentSize").GetInt64();
            string? url = obj.GetProperty("contentUrl").GetString();
            if (name == null || url == null) continue;
            dataObjects.Add(new DataObject(name, byte_size, url));
        }
        return dataObjects;
    }
}
internal class ScandDownloader : Downloader
{
    public async Task DownloadScand(List<DataObject> dataObjects, string store_path)
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
            await DownloadSingleFile(dataObject.url, filename);
        }
    }
}
