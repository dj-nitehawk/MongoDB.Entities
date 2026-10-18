## Bug fixes

- Fixed `Transaction.WithModifiedBy()` losing its session, so audited reads and writes now participate in the originating transaction and writes roll back on abort or disposal without commit.
- Fixed cascade deletion removing unrelated relationships when parent and child entities have overlapping IDs.
- File operations and cascading deletes now reject uppercase/mixed-case ObjectId-format file IDs before side effects, preventing them from overwriting or deleting a lowercase file's chunks. Existing chunk storage remains compatible; no data migration is required.
