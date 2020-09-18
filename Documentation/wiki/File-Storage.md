# GridFS alternative
this library features a GridFS alternative where you can stream upload & download files in chunks to keep memory usage at a minimum when dealing with large files. there is no limitation on the size or type of file you can store and the API is designed to be much simpler than GridFS.

### Define a file entity
inherit from `FileEntity` abstract class instead of the usual `Entity` class for defining your file entities like below. You can add any other properties you wish to store with it.

```csharp
public class Picture : FileEntity
{
    public string Title { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
```
the `FileEntity` is a sub class of `Entity` class. so all operations supported by the library can be performed with these file entities.

### Upload data
before uploading data for a file entity, you must save the file entity first. then simply call the upload method like below by supplying a stream object for it to read the data from:
```csharp
var kitty = new Picture
{
    Title = "NiceKitty.jpg",
    Width = 4000,
    Height = 4000
};

await kitty.SaveAsync();

var streamTask = new HttpClient()
                      .GetStreamAsync("https://placekitten.com/g/4000/4000");

using (var stream = await streamTask)
{
    await kitty.Data.UploadAsync(stream);
}
```
the `Data` property on the file entity gives you access to a couple of methods for uploading and downloading. with those methods, you can specify *upload chunk size*, *download batch size*, *operation timeout period*, as well as *cancellation token* for controlling the process.

in addition to the properties you added, there will also be `FileSize`, `ChunkCount` & `UploadSuccessful` properties on the file entity. the file size reports how much data has been read from the stream in bytes if the upload is still in progress or the total file size if the upload is complete. chunk count reports how many number of pieces the file has been broken into for storage. *UploadSuccessful* will only return true if the process completed without any issues.

### Download data
```csharp
var picture = await DB.Find<Picture>()
                      .Match(p => p.Title == "NiceKitty.jpg")
                      .ExecuteSingleAsync();

using (var stream = File.OpenWrite("kitty.jpg"))
{
    await picture.Data.DownloadAsync(stream);
}
```
first retrieve the file entity you want to work with and then call the `.Data.DownloadAsync()` method by supplying it a stream object to write the data to.

alternatively, if the ID of the file entity is known, you can avoid fetching the file entity from the database and access the data directly like so:
```csharp
await DB.File<Picture>("xxxxxxxxx").DownloadAsync(stream);
```

### Transaction support
uploading & downloading file data within a transaction requires passing in a session to the upload and download methods. see [here](Transactions.md#file-storage) for an example.
