using System;

namespace MongoDB.Entities;

/// <summary> 
/// Use this attribute on properties that you want to omit when using SavePreserving() instead of supplying an expression.  
/// TIP: These attribute decorations are only effective if you do not specify a preservation expression when calling SavePreserving()  
/// </summary> 
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class PreserveAttribute : Attribute { }

/// <summary> 
/// Properties that don't have this attribute will be omitted when using SavePreserving() 
/// TIP: These attribute decorations are only effective if you do not specify a preservation expression when calling SavePreserving() 
/// </summary> 
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class DontPreserveAttribute : Attribute { }