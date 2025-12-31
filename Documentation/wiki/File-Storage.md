## GridFS alternative

This library features a GridFS alternative where you can stream upload & download files in chunks to keep memory usage at a minimum when dealing with large files. There is no limitation on the size or type of file you can store and the API is designed to be much simpler than GridFS.

### Define a file entity

Inherit from `FileEntity<T>` abstract class instead of the usual `Entity` class for defining your file entities like below. You can add any other properties you wish to store with it.

```csharp
public class Picture : FileEntity<Picture>
{
    public string Title { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
```

The `FileEntity` is a sub class of `Entity` class. So all operations supported by the library can be performed with these file entities.

### Upload data

Before uploading data for a file entity, you must save the file entity first. Then simply call the upload method like below by supplying a stream object for it to read the data from:

```csharp
var kitty = new Picture
{
    Title = "NiceKitty.jpg",
    Width = 4000,
    Height = 4000
};
await db.SaveAsync(kitty);

var stream = await new HttpClient().GetStreamAsync("https://placekitten.com/g/4000/4000");
using (stream)
{
    await kitty.Data(db).UploadAsync(stream);
}
```

The `Data()` method on the file entity gives you access to a couple of options for uploading and downloading. With those methods, you can specify *upload chunk size*, *download batch size*, *operation timeout period*, as well as *cancellation token* for controlling the process.

In addition to the properties you added, there will also be `FileSize`, `ChunkCount` & `UploadSuccessful` properties on the file entity. The file size reports how much data has been read from the stream in bytes if the upload is still in progress or the total file size if the upload is complete. Chunk count reports how many number of pieces the file has been broken into for storage. *UploadSuccessful* will only return true if the process completed without any issues.

### Data integrity verification

You have the option of specifying an MD5 hash when uploading and get mongodb to throw an `InvalidDataException` in case the data stream has got corrupted during the upload/transfer process. Typically you'd calculate an MD5 hash value in your front-end/ui app before initiating the file upload and set it as a property value on the file entity like so:

```csharp
var kitty = new Picture
{
    Title = "NiceKitty.jpg",
    Width = 4000,
    Height = 4000,
    MD5 = "cccfa116f0acf41a217cbefbe34cd599"
};
```

The `MD5` property comes from the base `FileEntity`. If a value has been set before calling `.Data().UploadAsync()` an MD5 hash will be calculated at the end of the upload process and matched against the MD5 hash you specified. If they don't match, an exception is thrown. So if specifying an MD5 for verification, you should always wrap your upload code in a try/catch block. If verification fails, the uploaded data is discarded and you'll have to re-attempt the upload.

### Download data

```csharp
var picture = await db.Find<Picture>()
                      .Match(p => p.Title == "NiceKitty.jpg")
                      .ExecuteSingleAsync();

using (var stream = File.OpenWrite("kitty.jpg"))
{
    await picture.Data(db).DownloadAsync(stream);
}
```

First retrieve the file entity you want to work with and then call the `.Data(db).DownloadAsync()` method by supplying it a stream object to write the data to.

Alternatively, if the ID of the file entity is known, you can avoid fetching the file entity from the database and access the data directly like so:

```csharp
await db.File<Picture>("FileID").DownloadAsync(stream);
```

## Transaction support

Uploading & downloading file data within a transaction requires passing in a session to the upload and download methods. See [here](Transactions.md#file-storage) for an example.