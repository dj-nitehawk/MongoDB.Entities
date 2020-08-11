using System;

namespace MongoDB.Entities
{
    /// <summary>
    /// Implement this interface on entities you want the library to automatically store the modified date with
    /// </summary>
    public interface IModifiedOn
    {
        /// <summary>
        /// This property will be automatically set by the library when an entity is updated.
        /// <para>TIP: This property is useful when sorting by update date.</para>
        /// </summary>
        DateTime ModifiedOn { get; set; }
    }
}
