using System;

namespace MongoDB.Entities.Core
{
    /// <summary>
    /// Implement this interface on entities you want the library to automatically store the modified date with
    /// </summary>
    public interface IModifiedOn
    {
        /// <summary>
        /// This property will be automatically set when an entity is updated.
        /// <para>TIP: This property is useful when sorting by update date.</para>
        /// </summary>
        DateTime ModifiedOn { get; set; }
    }
}
