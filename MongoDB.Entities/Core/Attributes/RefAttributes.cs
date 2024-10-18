using System;

namespace MongoDB.Entities;

/// <summary>
/// Indicates that this property is the owner side of a many-to-many relationship
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class OwnerSideAttribute : Attribute;

/// <summary>
/// Indicates that this property is the inverse side of a many-to-many relationship
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class InverseSideAttribute : Attribute;