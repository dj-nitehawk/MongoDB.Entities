# Benchmark comparison with official driver

### Environment

```ini
MongoDB.Entities: v20.15.0
BenchmarkDotNet: v0.13.0
OS: Windows 10
CPU: AMD Ryzen 7 3700X
.NET SDK: 5.0.202
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