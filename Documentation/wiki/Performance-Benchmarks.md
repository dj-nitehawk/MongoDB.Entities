# Performance Benchmarks
the code for the benchmarks can be found under the `Benchmark` project on [github](https://github.com/dj-nitehawk/MongoDB.Entities/tree/master/Benchmark/Benchmarks).
more benchmarks will be added as time permits. in the meantime, please feel free to add your own and submit a PR on github. 

### Environment
```
OS     : Windows 10
CPU    : AMD Ryzen 7 3700X
SDK    : .Net 5.0
Server : MongoDB Community 4.4 hosted locally
```

## Create one entity

|           Method |     Mean |   Error |  StdDev | Ratio |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|----------------- |---------:|--------:|--------:|------:|-------:|-------:|------:|----------:|
| MongoDB_Entities | 261.7 μs | 3.86 μs | 3.61 μs |  0.98 | 3.4180 |      - |     - |     29 KB |
|  Official_Driver | 266.5 μs | 1.68 μs | 1.41 μs |  1.00 | 3.4180 | 0.4883 |     - |     29 KB |

## Bulk create 1000 entities

|           Method |     Mean |     Error |    StdDev | Ratio |   Gen 0 |   Gen 1 | Gen 2 | Allocated |
|----------------- |---------:|----------:|----------:|------:|--------:|--------:|------:|----------:|
| MongoDB_Entities | 9.722 ms | 0.1189 ms | 0.1054 ms |  0.99 | 78.1250 | 31.2500 |     - |    686 KB |
|  Official_Driver | 9.815 ms | 0.1480 ms | 0.1385 ms |  1.00 | 62.5000 | 31.2500 |     - |    582 KB |

## Find one entity

|           Method |     Mean |   Error |  StdDev |   Median | Ratio |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|----------------- |---------:|--------:|--------:|---------:|------:|-------:|-------:|------:|----------:|
| MongoDB_Entities | 266.1 μs | 1.93 μs | 1.80 μs | 266.6 μs |  0.99 | 3.4180 | 0.4883 |     - |     31 KB |
|  Official_Driver | 265.2 μs | 5.30 μs | 6.89 μs | 260.2 μs |  1.00 | 3.4180 | 0.4883 |     - |     31 KB |

## Find single entity

|           Method |     Mean |   Error |  StdDev | Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------- |---------:|--------:|--------:|------:|-------:|------:|------:|----------:|
| MongoDB_Entities | 263.4 μs | 5.10 μs | 5.87 μs |  0.99 | 3.4180 |     - |     - |     32 KB |
|  Official_Driver | 267.8 μs | 3.90 μs | 3.65 μs |  1.00 | 3.4180 |     - |     - |     31 KB |

## Find any entity 

> 3.7 times faster than driver

|           Method |       Mean |   Error |  StdDev |     Median | Ratio |   Gen 0 |   Gen 1 | Gen 2 | Allocated |
|----------------- |-----------:|--------:|--------:|-----------:|------:|--------:|--------:|------:|----------:|
| MongoDB_Entities |   292.1 μs | 5.84 μs | 8.18 μs |   297.2 μs |  0.27 |  3.9063 |  0.4883 |     - |     33 KB |
|  Official_Driver | 1,057.6 μs | 2.59 μs | 2.42 μs | 1,057.6 μs |  1.00 | 52.7344 | 13.6719 |     - |    446 KB |

## Find first entity

> 3.8 times faster than driver

|           Method |       Mean |    Error |  StdDev | Ratio |   Gen 0 |   Gen 1 | Gen 2 | Allocated |
|----------------- |-----------:|---------:|--------:|------:|--------:|--------:|------:|----------:|
| MongoDB_Entities |   269.6 μs |  2.60 μs | 2.43 μs |  0.26 |  3.4180 |       - |     - |     32 KB |
|  Official_Driver | 1,058.1 μs | 10.92 μs | 9.12 μs |  1.00 | 54.6875 | 13.6719 |     - |    446 KB |

## Find 100 entities

|           Method |     Mean |     Error |    StdDev | Ratio |   Gen 0 |  Gen 1 | Gen 2 | Allocated |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|------:|----------:|
| MongoDB_Entities | 1.056 ms | 0.0047 ms | 0.0044 ms |  1.01 | 54.6875 | 1.9531 |     - |    448 KB |
|  Official_Driver | 1.050 ms | 0.0060 ms | 0.0054 ms |  1.00 | 54.6875 | 1.9531 |     - |    447 KB |

## Update one entity

|           Method |     Mean |   Error |  StdDev |   Median | Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------- |---------:|--------:|--------:|---------:|------:|-------:|------:|------:|----------:|
| MongoDB_Entities | 258.6 μs | 1.75 μs | 1.55 μs | 258.6 μs |  1.00 | 3.4180 |     - |     - |     31 KB |
|  Official_Driver | 255.5 μs | 5.08 μs | 6.61 μs | 250.9 μs |  1.00 | 3.4180 |     - |     - |     32 KB |

## Update 100 entities

|           Method |     Mean |   Error |  StdDev | Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------- |---------:|--------:|--------:|------:|-------:|------:|------:|----------:|
| MongoDB_Entities | 277.8 μs | 5.20 μs | 5.11 μs |  1.02 | 3.9063 |     - |     - |     33 KB |
|  Official_Driver | 271.1 μs | 5.41 μs | 6.23 μs |  1.00 | 3.4180 |     - |     - |     32 KB |