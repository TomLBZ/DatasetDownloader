# DatasetDownloader
Downloads Huron and Scand datasets for general navigation models for robotics.

## Usage
```bash
# clone this repo
git clone https://github.com/TomLBZ/DatasetDownloader.git && cd DatasetDownloader
# edit configs
vim config.json
# run
dotnet run
```

## Development
Please inherit the `Downloader` class and implement the Download method for other types of datasets, and modify `Program.cs` accordingly.
You should also prepare the `linkFile`s as needed by scraping the database downloading websites.