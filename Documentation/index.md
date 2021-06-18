---
title: Welcome
---

![](images/social.png)

[![license](https://img.shields.io/github/license/dj-nitehawk/MongoDB.Entities?color=blue&label=license&logo=Github&style=flat-square)](https://github.com/dj-nitehawk/MongoDB.Entities/blob/master/README.md) [![nuget](https://img.shields.io/nuget/v/MongoDB.Entities?label=version&logo=NuGet&style=flat-square)](https://www.nuget.org/packages/MongoDB.Entities) [![nuget](https://img.shields.io/nuget/dt/MongoDB.Entities?color=blue&label=downloads&logo=NuGet&style=flat-square)](https://www.nuget.org/packages/MongoDB.Entities) [![tests](https://img.shields.io/azure-devops/tests/RyanGunner/MongoDB%20Entities/4?color=blue&label=tests&logo=Azure%20DevOps&style=flat-square)](https://dev.azure.com/RyanGunner/MongoDB%20Entities/_build/latest?definitionId=4) [![discord](https://img.shields.io/discord/768493765995921449?color=blue&label=discord&logo=discord&logoColor=white&style=flat-square)](https://discord.com/invite/CM5mw2G)

# What is it?
A light-weight .net standard library with [barely any overhead](wiki/Performance-Benchmarks.md) that aims to simplify access to mongodb by abstracting the official driver while adding useful features on top of it resulting in an elegant API surface which produces beautiful, human friendly data access code.

# Why use it?
- Async only API for scalable application development.
- Don't have to deal with `ObjectIds`, `BsonDocuments` & magic strings unless you want to.
- Built-in support for `One-To-One`, `One-To-Many` and `Many-To-Many` relationships.
- Query data using LINQ, lambda expressions, filters and aggregation pipelines.
- Sorting, paging and projecting is made convenient.
- Simple data migration framework similar to EntityFramework.
- Programatically manage indexes.
- Full text search (including fuzzy matching) with text indexes.
- Multi-document transaction support.
- Multiple database support.
- Easy bulk operations.
- Easy change-stream support.
- Easy audit fields support.
- GeoSpatial search.
- Global filters.
- Stream files in chunks to and from mongodb (GridFS alternative).
- Project types supported: .Net Standard 2.0 (.Net Core 2.0 & .Net Framework 4.6.1 or higher)

---

<div class="actions-container">
  <div><a href="wiki/Get-Started.md">Get Started</a></div>
  <div><a href="wiki/Code-Samples.md">Code Samples</a></div>
  <div><a href="wiki/Performance-Benchmarks.md">Benchmarks</a></div>
</div>

---

<div class="actions-container">
  <a href="https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=9LM2APQXVA9VE">
    <img src="images/donate.png" style="margin-top:20px;"/>
  </a>
</div>
