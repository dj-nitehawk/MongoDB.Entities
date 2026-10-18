## GridFS alternative

This library features a GridFS alternative where you can stream upload & download files in chunks to keep memory usage at a minimum when dealing with large files. There is no limitation on the size or type of file you can store and the API is designed to be much simpler than GridFS.

### Define a file entity

Inherit from `FileEntity<T>` abstract class instead of the usual `Entity` class for defining your file entities like below. You can add any other properties you wish to store with it.

```csharp
public class Picture : FileEntity<Picture>
{
    public string Title { get; set; } = "";
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

In addition to the properties you added, there will also be `FileSize`, `ChunkCount`, `UploadSuccessful`, and `BinaryStorageID` properties on the file entity. These are library-managed upload metadata. File size and chunk count describe the completed upload, not in-progress data. The storage reference and these statistics are switched together only after all chunks and checksum verification succeed. A failed replacement leaves the previous upload available. Do not edit these properties or overwrite them using raw updates or replacement operations. Normal full and partial saves preserve the persisted upload metadata, including when saving a projection or stale entity.

Saving or inserting a loaded file under a new ID copies its application metadata, not its binary content. This includes resetting the ID to generate a new one and single or batch operations. The newly inserted file starts with no storage reference, zero size/chunks, and `UploadSuccessful == false`; upload its content separately. Saving an existing identity still preserves that identity's persisted upload metadata.

### Data integrity verification

You can set the `Md5` property before uploading to have the library verify the stream's checksum. The library throws an `InvalidDataException` if the calculated hash does not match. For example, calculate the expected hash before initiating the upload and set it on the file entity:

```csharp
var kitty = new Picture
{
    Title = "NiceKitty.jpg",
    Width = 4000,
    Height = 4000,
    Md5 = "cccfa116f0acf41a217cbefbe34cd599"
};
```

The `Md5` property comes from `FileEntity<T>`. Set it before calling `.Data(db).UploadAsync()`. If verification fails, the uploaded data is discarded and you'll need to handle the exception and retry the upload. This checksum detects accidental corruption, not malicious tampering.

### Download data

```csharp
var picture = await db.Find<Picture>()
                      .Match(p => p.Title == "NiceKitty.jpg")
                      .ExecuteSingleAsync();

if (picture is null)
    throw new InvalidOperationException("Picture not found.");

using (var stream = File.OpenWrite("kitty.jpg"))
{
    await picture.Data(db).DownloadAsync(stream);
}
```

First retrieve the file entity you want to work with and then call the `.Data(db).DownloadAsync()` method by supplying it a stream object to write the data to.

Alternatively, if the ID of the file entity is known, access the data directly like so (custom string IDs are also supported):

```csharp
await db.File<Picture>("FileID").DownloadAsync(stream);
```

Streaming operations resolve the current persisted metadata before accessing chunks. This also lets projected entities and objects used in an aborted transaction resolve the correct stored upload. Concurrent replacements are conditional: only one upload can replace a given storage reference. A losing upload throws `InvalidOperationException`; reload and retry. Concurrent entity deletion can return zero if its captured storage reference has changed; retry if deletion is still desired. Downloads that overlap a successful replacement or deletion are not snapshot-isolated unless performed in a transaction.

### Storage format and upgrading

New uploads use a fresh, random `BinaryStorageID` independent of the entity ID. New chunks contain that reference and omit the legacy `FileID` field, so files in different collections cannot alias each other's new uploads. Explicit chunk deletion also persists cleared upload statistics.

**Upgrade every application instance before enabling uploads with this version.** Older versions cannot read the new references and are unsafe to run alongside new-format writers. No bulk rewrite or migration command is required. Existing metadata without `BinaryStorageID` continues to read legacy chunks using `FileID`, including its existing BSON ObjectId representation. Do not convert legacy chunk `FileID` values to strings.

Legacy files with overlapping IDs are not automatically repaired. Replacing a legacy upload retains its old chunks because another collection may still own them. Legacy destructive operations inspect other database collections for conflicting file metadata, ignoring application global filters, and reject established conflicts before deleting. This requires collection-listing and read access across the database, and fails if that inventory cannot be read. It is not an ownership proof against simultaneous legacy writes or unrecognizable/custom legacy schemas. Reconcile ambiguous files explicitly, with backups and a write freeze. Already overwritten bytes cannot be recovered.

Interrupted uploads, unknown network outcomes, and retained legacy data can leave orphan chunks. Do not delete them by guessing ownership from entity IDs. Raw collection access and `Replace` bypass the managed-save protections; preserve all upload metadata when using them.

Storage references already shared by earlier metadata copies are not automatically reconciled. Back up affected data and reconcile ownership with writes stopped before deleting or replacing any affected file. Do not delete shared chunks while repairing metadata; chunks already deleted cannot be restored by resetting metadata.

## Transaction support

Uploading & downloading file data within a transaction requires passing in a session to the upload and download methods. See [here](Transactions.md#file-storage) for an example.