![](images/social.png)

[![nuget](https://img.shields.io/nuget/v/MongoDB.Entities?label=version&logo=NuGet&style=flat-square)](https://www.nuget.org/packages/MongoDB.Entities) [![nuget](https://img.shields.io/nuget/dt/MongoDB.Entities?color=blue&label=downloads&logo=NuGet&style=flat-square)](https://www.nuget.org/packages/MongoDB.Entities) [![tests](https://img.shields.io/azure-devops/tests/RyanGunner/MongoDB%20Entities/1?color=blue&label=tests&logo=Azure%20DevOps&style=flat-square)](https://dev.azure.com/RyanGunner/MongoDB%20Entities/_build/latest?definitionId=1) [![license](https://img.shields.io/github/license/dj-nitehawk/MongoDB.Entities?color=blue&label=license&logo=Github&style=flat-square)](https://github.com/dj-nitehawk/MongoDB.Entities/blob/master/README.md)

# What is it?
A light-weight .net standard library which simplifies access to mongodb by abstracting away the official .net mongodb driver and providing some additional features on top of it. The API is clean and intuitive resulting in less lines of code that is more human friendly than driver code.

# Why use it?
- Don't have to deal with `ObjectIds`, `BsonDocuments` & magic strings unless you want to.
- Built-in support for `One-To-One`, `One-To-Many` and `Many-To-Many` relationships.
- Async only API for scalable application development.
- Query data using LINQ, lambda expressions, filters and aggregation pipelines.
- Sorting, paging and projecting is made convenient.
- Simple data migration framework similar to EntityFramework.
- Programatically define indexes.
- Full text search (including fuzzy matching) with text indexes.
- Multi-document transaction support.
- Multiple database support.
- Easy bulk operations.
- Easy Change-stream support.
- GeoSpatial search.
- Stream files in chunks to and from mongodb (GridFS alternative).
- Project types supported: .Net Standard 2.0 (.Net Core 2.0 & .Net Framework 4.6.1 or higher)
---
# [Get Started >>](wiki/Get-Started.md)
# [View Code Samples >>](wiki/Code-Samples.md)
---
# Donations
If this library has made your life easier and you'd like to express your gratitude, you can donate a couple of bucks via paypal by clicking below:

[![](https://www.paypalobjects.com/en_US/i/btn/btn_donate_LG.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=9LM2APQXVA9VE)

