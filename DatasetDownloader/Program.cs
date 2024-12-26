using System.Text.Json;
using DatasetDownloader;

// look for config.json. If it does not exist, create it with default values.
const string json_template = @"{
  ""datasets"": [
    {
      ""link_file"": ""<DATASET_SPECIFIC_LINKS>"",
      ""name"": ""<DATASET_NAME>"",
      ""path"": ""<DOWNLOAD_PATH>"",
      ""url"": ""<BASE_URL>""
    }
  ]
}";

string[] supportedDatasetTypes = { "HURON", "SCAND" };

if (!File.Exists("config.json"))
{
    File.WriteAllText("config.json", json_template);
    Console.WriteLine("Created config.json. Please edit it with the appropriate values, and then try again.");
    return;
}

// Load the configuration file
using JsonDocument doc = JsonDocument.Parse(File.ReadAllText("config.json"));
JsonElement root = doc.RootElement;
JsonElement datasets = root.GetProperty("datasets");
List<Task> tasks = [];
foreach (JsonElement dataset in datasets.EnumerateArray())
{
    string? name = dataset.GetProperty("name").GetString()?.ToUpper();
    if (name == null || !supportedDatasetTypes.Contains(name))
    {
        Console.WriteLine("Invalid dataset name. Please check the config.json file.");
        return;
    }
    string? link_file = dataset.GetProperty("link_file").GetString();
    string? path = dataset.GetProperty("path").GetString();
    string? url = dataset.GetProperty("url").GetString();
    if (link_file == null || path == null || url == null)
    {
        Console.WriteLine("Invalid configuration. Please check the config.json file.");
        return;
    }
    switch (name)
    {
        case "HURON":
            HuronDownloader downloader = new(url);
            Task<List<string>> huronLinksTask = downloader.GetHuronPageLinks(link_file);
            Task huronDownloadTask = huronLinksTask.ContinueWith(links => downloader.DownloadHuron(links.Result, path));
            tasks.Add(huronDownloadTask);
            break;
        case "SCAND":
            ScandDownloader scandDownloader = new();
            List<DataObject> dataObjects = DataObject.Load(link_file);
            Task scandDownloadTask = scandDownloader.DownloadScand(dataObjects, path);
            tasks.Add(scandDownloadTask);
            break;
        default:
            Console.WriteLine("Invalid dataset name. Please check the config.json file.");
            return;
    }
    Console.WriteLine($"Downloading datasets of type {name} to {path}");
}

// await task one after another
int taskIndex = 0;
foreach (var task in tasks)
{
    Console.WriteLine($"======= Downloading datasets {++taskIndex}/{tasks.Count} =======");
    task.RunSynchronously();
}