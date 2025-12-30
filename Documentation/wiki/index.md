![](/images/header.svg)

[![license](https://img.shields.io/github/license/dj-nitehawk/MongoDB.Entities?color=blue&label=license&logo=Github&style=flat-square)](https://github.com/dj-nitehawk/MongoDB.Entities/blob/master/README.md) [![nuget](https://img.shields.io/nuget/v/MongoDB.Entities?label=version&logo=NuGet&style=flat-square)](https://www.nuget.org/packages/MongoDB.Entities) [![nuget](https://img.shields.io/nuget/dt/MongoDB.Entities?color=blue&label=downloads&logo=NuGet&style=flat-square)](https://www.nuget.org/packages/MongoDB.Entities) [![tests](https://img.shields.io/azure-devops/tests/RyanGunner/MongoDB%20Entities/4?color=blue&label=tests&logo=Azure%20DevOps&style=flat-square)](https://dev.azure.com/RyanGunner/MongoDB%20Entities/_build/latest?definitionId=4) [![discord](https://img.shields.io/discord/768493765995921449?color=blue&label=discord&logo=discord&logoColor=white&style=flat-square)](https://discord.gg/8UpqWT35rj)

# What is it?

A light-weight `.NET Standard` library with [barely any overhead](Performance-Benchmarks.md) that aims to simplify access to mongodb by abstracting the official driver while adding useful features on top of it resulting in an elegant API surface which produces beautiful, human friendly data access code.

# Why use it?

- Async only API for scalable application development.
- Don't have to deal with `ObjectIds`, `BsonDocuments` & magic strings unless you want to.
- Built-in support for `One-To-One`, `One-To-Many` and `Many-To-Many` relationships.
- Query data using LINQ, lambda expressions, filters and aggregation pipelines.
- Sorting, paging and projecting is made convenient.
- Simple data migration framework similar to EntityFramework.
- Programmatically manage indexes.
- Full text search (including fuzzy matching) with text indexes.
- Multi-document transaction support.
- Multiple database support.
- Easy bulk operations.
- Easy change-stream support.
- Easy audit fields support.
- GeoSpatial search.
- Global filters.
- Stream files in chunks to and from mongodb (GridFS alternative).
- Minimum TFM: .Net Standard 2.1

---

<div style="display:flex;justify-content:left;gap:12px;margin:0;">
  <a href="Get-Started.md" style="display:inline-block;padding:10px 16px;background:#0078D4;color:#fff;border-radius:6px;text-decoration:none;font-weight:600;box-shadow:0 2px 4px rgba(0,0,0,0.1);">Get Started</a>
  <a href="Code-Samples.md" style="display:inline-block;padding:10px 16px;background:#0A6ED1;color:#fff;border-radius:6px;text-decoration:none;font-weight:600;box-shadow:0 2px 4px rgba(0,0,0,0.1);">Code Samples</a>
  <a href="Performance-Benchmarks.md" style="display:inline-block;padding:10px 16px;background:#0A6ED1;color:#fff;border-radius:6px;text-decoration:none;font-weight:600;box-shadow:0 2px 4px rgba(0,0,0,0.1);">Benchmarks</a>
</div>