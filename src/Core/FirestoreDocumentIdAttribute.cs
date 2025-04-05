using System;

namespace FireSchema.CS.Runtime.Core
{
    /// <summary>
    /// Attribute used to mark the property on a model class that should hold the Firestore document ID.
    /// The FirestoreDataConverter uses this attribute to map the document ID during conversion.
    /// This property itself is NOT stored within the Firestore document's fields.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class FirestoreDocumentIdAttribute : Attribute
    {
        // This attribute doesn't need any properties for now.
        // Its presence on a property is sufficient for identification.
    }
}