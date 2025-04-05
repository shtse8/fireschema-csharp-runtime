using Google.Cloud.Firestore;

namespace FireSchema.CS.Runtime.Core
{
    /// <summary>
    /// Interface implemented by all generated Firestore document model classes.
    /// </summary>
    public interface IFirestoreDocument
    {
        /// <summary>
        /// The document ID. This property is typically populated by the runtime
        /// after fetching the document and is not stored directly within the
        /// Firestore document's fields. It should be marked with the
        /// [FirestoreDocumentId] attribute in implementing classes.
        /// </summary>
        string Id { get; set; }

        // Potentially add other common properties or methods needed by the runtime later.
        // For example:
        // DocumentReference SelfReference { get; set; } // If needed for easy access
    }
}