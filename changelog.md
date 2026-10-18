## Bug fixes

<details>
<summary>Retry file uploads after an index setup failure</summary>

If index setup fails during an upload, you can now retry without restarting your application or recreating the client. Later uploads are no longer stuck behind the failed attempt.

</details>

<details>
<summary>Custom BSON field names no longer leave file data behind</summary>

If you rename upload-reference fields through BSON mappings or naming conventions, deleting a file now removes its uploaded chunks too. Full-save upserts also respect those names when setting up empty upload metadata.

</details>

<details>
<summary>Full file saves correctly remove cleared metadata</summary>

Clearing optional file metadata, such as an `Md5` checksum or a `[BsonExtraElements]` entry, now removes it from the database on a full save. Your uploaded data and upload statistics stay intact. This fixes both single and batch saves; partial saves are unchanged.

</details>

<details>
<summary>Saving a file under a new ID no longer puts the original upload at risk</summary>

If you save or insert a loaded file under a new ID, it now starts with empty upload metadata rather than sharing the original's binary data. Deleting, clearing, or uploading to the copy no longer removes the original's chunks. Binary content is not copied, so upload it separately if needed.

Existing shared references are not automatically repaired. See the [file-storage upgrade guidance](Documentation/wiki/File-Storage.md#storage-format-and-upgrading) before handling affected data.

</details>

<details>
<summary>Files kept after a concurrent upload retain their relationships</summary>

If a replacement upload causes a file deletion to be skipped, the surviving file now keeps its relationships. Batch deletes only clean up relationships for files actually deleted.

</details>

<details>
<summary>Files in different collections can safely share the same ID</summary>

Files now have independent upload references, so matching IDs in different collections no longer cause them to overwrite or delete each other's data. Failed replacements preserve the previous upload, and conflicting concurrent changes are rejected.

Existing files remain readable without migration. **Upgrade every application instance before allowing new uploads.** See [file-storage compatibility](Documentation/wiki/File-Storage.md#storage-format-and-upgrading) for the rollout and safety guidance.

</details>

<details>
<summary>Audited operations stay inside their transaction</summary>

Using `Transaction.WithModifiedBy()` no longer loses the transaction's session. Your audited reads and writes now participate in the original transaction, and writes roll back when you abort or dispose without committing.

</details>

<details>
<summary>Cascade deletes preserve unrelated relationships with matching IDs</summary>

If parent and child entities have overlapping IDs, deleting one no longer removes unrelated relationships belonging to the other.

</details>

<details>
<summary>File operations reject ambiguous ObjectId-format IDs before changing data</summary>

File operations and cascading deletes now reject uppercase or mixed-case ObjectId-format file IDs before making changes. This prevents them from overwriting or deleting a lowercase file's chunks. Use lowercase ObjectId-format file IDs; non-hex custom IDs remain supported.

Existing chunk storage stays compatible, and no data migration is required. This guard does not repair data already affected by earlier versions. See [file-storage compatibility](Documentation/wiki/File-Storage.md#storage-format-and-upgrading) for details.

</details>
