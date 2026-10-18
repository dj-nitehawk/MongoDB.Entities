## Bug fixes

- File uploads can retry index initialization after a failure instead of remaining blocked for the lifetime of the client/database cache.
- File deletion now respects custom BSON names for upload references, preventing successful metadata deletion from leaving uploaded chunks behind. Full-save upserts also use mapped names when initializing empty upload metadata, including naming conventions.
- Full file saves now remove optional metadata omitted by BSON serialization, including a cleared `Md5` checksum and deleted `[BsonExtraElements]` entries, while preserving existing binary references and upload statistics. Single and batch saves are fixed; partial-save behavior is unchanged.
- Saving or inserting a loaded file under a new ID now starts with empty upload metadata instead of sharing the source's binary data. Deleting, clearing, or uploading to the copy no longer removes the source's chunks. Existing shared references are not automatically repaired.
- File deletes skipped after a concurrent replacement upload now preserve the surviving file's relationships. Mixed batches clean up relationships only for files actually deleted.
- File uploads now use independent binary-storage references, preventing files in different collections with the same ID from overwriting or deleting each other's data. Replacements preserve the previous upload on failure and reject concurrent changes. Legacy files remain readable without migration; upgrade every application instance before allowing new uploads. See [file-storage compatibility](Documentation/wiki/File-Storage.md#storage-format-and-upgrading).
- Fixed `Transaction.WithModifiedBy()` losing its session, so audited reads and writes now participate in the originating transaction and writes roll back on abort or disposal without commit.
- Fixed cascade deletion removing unrelated relationships when parent and child entities have overlapping IDs.
- File operations and cascading deletes now reject uppercase/mixed-case ObjectId-format file IDs before side effects, preventing them from overwriting or deleting a lowercase file's chunks. Existing chunk storage remains compatible; no data migration is required.
