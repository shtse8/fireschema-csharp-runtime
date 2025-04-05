using Google.Cloud.Firestore;
using System;
using System.Threading.Tasks; // For async methods
using System.Collections.Generic; // For IEnumerable

namespace FireSchema.CS.Runtime.Core
{
    /// <summary>
    /// Base class for generated Firestore collection references.
    /// Provides common functionality for interacting with a collection.
    /// </summary>
    /// <typeparam name="T">The type of the document data (must implement IFirestoreDocument).</typeparam>
    public abstract class BaseCollectionRef<T> where T : class, IFirestoreDocument, new()
    {
        /// <summary>
        /// Gets the underlying Firestore CollectionReference.
        /// </summary>
        public CollectionReference FirestoreCollection { get; }

        /// <summary>
        /// Gets the FirestoreDb instance associated with this collection.
        /// </summary>
        public FirestoreDb FirestoreDb => FirestoreCollection.Database;

        /// <summary>
        /// Initializes a new instance of the BaseCollectionRef class.
        /// </summary>
        /// <param name="firestoreDb">The Firestore database instance.</param>
        /// <param name="collectionPath">The path to the Firestore collection.</param>
        protected BaseCollectionRef(FirestoreDb firestoreDb, string collectionPath)
        {
            if (firestoreDb == null) throw new ArgumentNullException(nameof(firestoreDb));
            if (string.IsNullOrEmpty(collectionPath)) throw new ArgumentNullException(nameof(collectionPath));

            // TODO: Potentially add validation for collectionPath format?

            FirestoreCollection = firestoreDb.Collection(collectionPath); // Remove WithConverter
        }

        /// <summary>
        /// Gets a DocumentReference for the document within this collection
        /// with the specified document ID. Does not fetch the document.
        /// </summary>
        /// <param name="documentId">The document ID.</param>
        /// <returns>A DocumentReference.</returns>
        public virtual DocumentReference Doc(string documentId)
        {
            if (string.IsNullOrEmpty(documentId)) throw new ArgumentNullException(nameof(documentId));
            return FirestoreCollection.Document(documentId);
        }

        /// <summary>
        /// Asynchronously retrieves the document with the specified ID.
        /// </summary>
        /// <param name="documentId">The ID of the document to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the document snapshot.</returns>
        public virtual async Task<DocumentSnapshot> GetDocSnapshotAsync(string documentId)
        {
            return await Doc(documentId).GetSnapshotAsync();
        }

        /// <summary>
        /// Asynchronously retrieves the document with the specified ID and converts it to the specified type T.
        /// </summary>
        /// <param name="documentId">The ID of the document to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the document data, or null if the document doesn't exist.</returns>
        // Instance of the converter for manual use
        private static readonly FirestoreDataConverter<T> _converter = new FirestoreDataConverter<T>();

        public virtual async Task<T?> GetAsync(string documentId)
        {
            var snapshot = await GetDocSnapshotAsync(documentId);
            // Manually call the internal converter method that handles ID
            return _converter.FromFirestore(snapshot);
        }

        /// <summary>
        /// Asynchronously adds a new document to this collection with the specified data.
        /// Firestore will generate the document ID.
        /// </summary>
        /// <param name="data">The data for the new document. The Id property will be ignored.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the DocumentReference of the newly created document.</returns>
        public virtual async Task<DocumentReference> AddAsync(T data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            // Firestore automatically generates the ID, ensure local Id isn't sent if converter doesn't handle it.
            // The FirestoreDataConverter should ideally handle not serializing the Id property.
            return await FirestoreCollection.AddAsync(data);
        }

        /// <summary>
        /// Asynchronously creates or overwrites the document with the specified ID.
        /// </summary>
        /// <param name="documentId">The ID of the document to set.</param>
        /// <param name="data">The data for the document. The Id property should match documentId or be ignored by the converter.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the WriteResult.</returns>
        public virtual async Task<WriteResult> SetAsync(string documentId, T data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            // Ensure the Id property on the data object matches the documentId if necessary,
            // although the converter should handle this.
            data.Id = documentId; // Set it just in case
            return await Doc(documentId).SetAsync(data);
        }

        /// <summary>
        /// Asynchronously creates or merges the document with the specified ID.
        /// </summary>
        /// <param name="documentId">The ID of the document to set.</param>
        /// <param name="data">The data to merge into the document.</param>
        /// <param name="options">Merge options (e.g., MergeAll, MergeFields).</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the WriteResult.</returns>
        public virtual async Task<WriteResult> SetAsync(string documentId, object data, SetOptions options)
        {
             if (data == null) throw new ArgumentNullException(nameof(data));
             // Use object here for merging flexibility, as T might not represent partial data well.
             return await Doc(documentId).SetAsync(data, options);
        }


        // --- TODO: Add UpdateAsync methods ---
        // public virtual Task<WriteResult> UpdateAsync(string documentId, /* Update data representation */) { ... }

        // --- TODO: Add DeleteAsync method ---
        // public virtual Task<WriteResult> DeleteAsync(string documentId) { ... }

        // --- TODO: Add Query methods (returning a QueryBuilder) ---
        // public virtual BaseQueryBuilder<T> Where(...) { ... }
        // public virtual BaseQueryBuilder<T> OrderBy(...) { ... }
        // public virtual BaseQueryBuilder<T> Limit(...) { ... }

    }
}