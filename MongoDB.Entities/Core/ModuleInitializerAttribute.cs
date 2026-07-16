// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

//netstandard2.1 doesn't ship this attribute; the compiler only needs it to exist by name.
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
sealed class ModuleInitializerAttribute : Attribute;
