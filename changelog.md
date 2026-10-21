## Improvements

<details>
<summary>Fewer database requests for filtered batch deletes without relationships</summary>

Deleting non-file entities with no registered relationships now uses one bulk delete per batch instead of a separate delete request for every candidate. Global filters are still checked at deletion time. Initialize relationships before running concurrent deletes; see the [global-filter guidance](Documentation/wiki/Global-Filters.md).

</details>

## Bug fixes

<details>
<summary>Copied databases no longer change the source's tenant filters</summary>

Creating a derived database from an already-filtered instance now keeps their filter registrations independent. Configuring the copy no longer redirects the source's queries, updates, or deletes to another tenant. Existing audit views and transactions keep their filters too, including reads performed by your hooks. See the [global-filter guidance](Documentation/wiki/Global-Filters.md#copying-database-instances).

</details>

<details>
<summary>Partial writes no longer persist properties excluded by your BSON mappings</summary>

Partial saves and entity-based updates now honor properties excluded by class maps or conventions, just like full saves. They also leave class-map-configured identities out of updates. This prevents transient or sensitive values from being written unexpectedly. Previously stored fields are not removed automatically; review affected data and plan a deliberate cleanup. See the [partial-save guidance](Documentation/wiki/Entities-Save.md#save-entities-partially).

</details>

<details>
<summary>Transactions keep your custom save and update hooks</summary>

Starting a transaction directly from your derived database no longer skips validation, normalization, or hook-managed audit logic. Your hooks run with the transaction's session, without needing to call `WithModifiedBy()` first. See the [transaction guidance](Documentation/wiki/Transactions.md#custom-hooks).

</details>

<details>
<summary>Pipeline updates no longer skip your update hooks</summary>

Your `OnBeforeUpdate` action now runs before pipeline updates from both update builders, including audit views. A rejecting hook prevents the write. If your hook adds regular modifications, pipeline execution now throws `ArgumentException` instead of silently ignoring them. Use regular updates with modifying hooks. See the [event-hook guidance](Documentation/wiki/Event-Hooks.md#pipeline-updates).

</details>

<details>
<summary>Entities skipped by a concurrent filter change keep their relationships</summary>

If an entity becomes excluded by a global filter while you are deleting it, its relationships now stay intact. Mixed batches clean up relationships only for entities actually deleted. Filtered non-file deletes with registered relationships use one conditional delete per candidate, so large filtered deletes of related entities may take longer. Use a transaction if entity and relationship deletion must roll back together on failure. See the [global-filter guidance](Documentation/wiki/Global-Filters.md).

</details>

<details>
<summary>Audit views keep your custom write hooks</summary>

Calling `WithModifiedBy()` on a derived database no longer skips your save and update hooks. Your normalization and validation still run, and hooks see the audit user supplied to that view without changing the original instance. Chained views and transactions created from an audited view retain the hooks too.

</details>

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
