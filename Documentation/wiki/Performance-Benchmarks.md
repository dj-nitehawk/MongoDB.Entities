# Performance Benchmarks
source code of the benchmarks can be found on [github](https://github.com/dj-nitehawk/MongoDB.Entities/tree/master/Benchmark/Benchmarks).
more benchmarks will be added as time permits. please feel free to add your own and submit a PR, or join our [discord server](https://discord.com/invite/CM5mw2G) and request a particular benchmark you're interested in.

### Environment
```
OS     : Windows 10
CPU    : AMD Ryzen 7 3700X
SDK    : .Net 5.0
Server : MongoDB Community 4.4 hosted locally
```

## Create one entity

|           Method |     Mean |   Error |  StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------- |---------:|--------:|--------:|------:|--------:|-------:|------:|------:|----------:|
|  Official_Driver | 235.0 μs | 2.82 μs | 2.50 μs |  1.00 |    0.00 | 3.4180 |     - |     - |     29 KB |
| MongoDB_Entities | 259.1 μs | 1.69 μs | 1.50 μs |  1.10 |    0.02 | 3.4180 |     - |     - |     29 KB |

## Bulk create 1000 entities

|           Method |      Mean |     Error |    StdDev |    Median | Ratio | RatioSD |   Gen 0 |   Gen 1 | Gen 2 | Allocated |
|----------------- |----------:|----------:|----------:|----------:|------:|--------:|--------:|--------:|------:|----------:|
| MongoDB_Entities |  9.627 ms | 0.1034 ms | 0.0863 ms |  9.620 ms |  0.98 |    0.04 | 78.1250 | 31.2500 |     - |    686 KB |
|  Official_Driver | 10.558 ms | 0.2091 ms | 0.4546 ms | 10.732 ms |  1.00 |    0.00 | 62.5000 | 31.2500 |     - |    582 KB |

## Find one entity

|           Method |     Mean |   Error |  StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------- |---------:|--------:|--------:|------:|--------:|-------:|------:|------:|----------:|
| MongoDB_Entities | 254.8 μs | 2.25 μs | 1.76 μs |  0.96 |    0.03 | 3.4180 |     - |     - |     31 KB |
|  Official_Driver | 265.8 μs | 5.05 μs | 6.01 μs |  1.00 |    0.00 | 3.4180 |     - |     - |     31 KB |

## Find single entity

|           Method |     Mean |   Error |  StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------- |---------:|--------:|--------:|------:|--------:|-------:|------:|------:|----------:|
|  Official_Driver | 254.1 μs | 4.63 μs | 4.33 μs |  1.00 |    0.00 | 3.6621 |     - |     - |     31 KB |
| MongoDB_Entities | 261.1 μs | 5.14 μs | 4.81 μs |  1.03 |    0.03 | 3.4180 |     - |     - |     32 KB |

## Find any entity 

|           Method |       Mean |   Error |  StdDev | Ratio |   Gen 0 |   Gen 1 | Gen 2 | Allocated |
|----------------- |-----------:|--------:|--------:|------:|--------:|--------:|------:|----------:|
| MongoDB_Entities |   270.1 μs | 4.58 μs | 4.29 μs |  0.26 |  3.9063 |       - |     - |     33 KB |
|  Official_Driver | 1,026.6 μs | 5.27 μs | 4.68 μs |  1.00 | 52.7344 | 13.6719 |     - |    446 KB |

## Find first entity

|           Method |       Mean |   Error |  StdDev | Ratio |   Gen 0 |   Gen 1 | Gen 2 | Allocated |
|----------------- |-----------:|--------:|--------:|------:|--------:|--------:|------:|----------:|
| MongoDB_Entities |   257.0 μs | 5.02 μs | 6.87 μs |  0.26 |  3.4180 |       - |     - |     32 KB |
|  Official_Driver | 1,011.3 μs | 7.42 μs | 6.94 μs |  1.00 | 54.6875 | 13.6719 |     - |    446 KB |

## Find 100 entities

|           Method |     Mean |     Error |    StdDev | Ratio |   Gen 0 |   Gen 1 | Gen 2 | Allocated |
|----------------- |---------:|----------:|----------:|------:|--------:|--------:|------:|----------:|
| MongoDB_Entities | 1.028 ms | 0.0025 ms | 0.0024 ms |  1.00 | 54.6875 | 13.6719 |     - |    448 KB |
|  Official_Driver | 1.032 ms | 0.0087 ms | 0.0077 ms |  1.00 | 54.6875 | 13.6719 |     - |    447 KB |

## Update one entity

|           Method |     Mean |   Error |  StdDev | Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------- |---------:|--------:|--------:|------:|-------:|------:|------:|----------:|
| MongoDB_Entities | 234.2 μs | 2.47 μs | 2.19 μs |  0.95 | 3.6621 |     - |     - |     31 KB |
|  Official_Driver | 246.1 μs | 1.36 μs | 1.13 μs |  1.00 | 3.4180 |     - |     - |     32 KB |

## Update 100 entities

|           Method |     Mean |   Error |  StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------- |---------:|--------:|--------:|------:|--------:|-------:|------:|------:|----------:|
|  Official_Driver | 262.3 μs | 5.01 μs | 5.57 μs |  1.00 |    0.00 | 3.4180 |     - |     - |     32 KB |
| MongoDB_Entities | 272.9 μs | 5.01 μs | 4.68 μs |  1.04 |    0.04 | 3.9063 |     - |     - |     33 KB |

## Manual update vs. save partial

|      Method |     Mean |   Error |  StdDev | Ratio |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|------------ |---------:|--------:|--------:|------:|-------:|-------:|------:|----------:|
|      Update | 236.0 μs | 2.22 μs | 1.85 μs |  1.00 | 3.9063 |      - |     - |     33 KB |
| SavePartial | 392.5 μs | 1.47 μs | 1.37 μs |  1.66 | 4.8828 | 1.9531 |     - |     41 KB |

## DBContext vs. DB static save

|     Method |     Mean |   Error |  StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------- |---------:|--------:|--------:|------:|--------:|-------:|------:|------:|----------:|
| DB_Context | 246.2 μs | 3.72 μs | 3.30 μs |  0.94 |    0.02 | 2.9297 |     - |     - |     26 KB |
|  DB_Static | 262.7 μs | 5.24 μs | 8.90 μs |  1.00 |    0.00 | 2.9297 |     - |     - |     26 KB |

## Relationships

|             Method |       Mean |    Error |   StdDev | Ratio | RatioSD |   Gen 0 |  Gen 1 | Gen 2 | Allocated |
|------------------- |-----------:|---------:|---------:|------:|--------:|--------:|-------:|------:|----------:|
|             Lookup |   443.1 μs |  2.30 μs |  2.15 μs |  1.00 |    0.00 |  5.3711 |      - |     - |     44 KB |
|    Clientside_Join |   555.2 μs |  4.10 μs |  3.63 μs |  1.25 |    0.01 |  8.7891 |      - |     - |     73 KB |
|    Children_Fluent | 1,265.3 μs | 20.58 μs | 19.25 μs |  2.86 |    0.05 |  7.8125 |      - |     - |     76 KB |
| Children_Queryable | 1,547.5 μs |  6.18 μs |  4.83 μs |  3.50 |    0.02 | 11.7188 | 3.9063 |     - |    107 KB |