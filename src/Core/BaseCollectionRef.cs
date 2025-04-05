using Google.Cloud.Firestore;
using System;
using System.Threading.Tasks; // For async methods
using System.Collections.Generic; // For IEnumerable
using System.Linq.Expressions; // For query methods

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


        /// <summary>
        /// Asynchronously updates the document with the specified ID using a builder pattern.
        /// </summary>
        /// <param name="documentId">The ID of the document to update.</param>
        /// <param name="updateAction">An action that configures the update operations on a builder.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the WriteResult.</returns>
        public virtual async Task<WriteResult> UpdateAsync(string documentId, Action<BaseUpdateBuilder<T>> updateAction)
        {
            if (string.IsNullOrEmpty(documentId)) throw new ArgumentNullException(nameof(documentId));
            if (updateAction == null) throw new ArgumentNullException(nameof(updateAction));

            var docRef = Doc(documentId);
            // Note: This creates the base builder. Generated code might override this
            // to provide a more specific builder if needed, but the base works.
            var builder = new BaseUpdateBuilder<T>(docRef);
            updateAction(builder); // Configure the builder via the provided action

            return await builder.ApplyAsync(); // Apply the updates
        }

        /// <summary>
        /// Asynchronously deletes the document with the specified ID.
        /// </summary>
        /// <param name="documentId">The ID of the document to delete.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the WriteResult.</returns>
        public virtual async Task<WriteResult> DeleteAsync(string documentId)
        {
            if (string.IsNullOrEmpty(documentId)) throw new ArgumentNullException(nameof(documentId));
            return await Doc(documentId).DeleteAsync();
        }

        // --- Query Methods ---

        /// <summary>
        /// Creates a query against the collection.
        /// </summary>
        /// <returns>A BaseQueryBuilder instance for constructing the query.</returns>
        public virtual BaseQueryBuilder<T> Query()
        {
            // Pass the base CollectionReference to the builder
            return new BaseQueryBuilder<T>(FirestoreCollection);
        }

        /// <summary>
        /// Creates a query that filters the documents based on a specified field, operator, and value.
        /// </summary>
        public virtual BaseQueryBuilder<T> Where<TField>(Expression<Func<T, TField>> fieldSelector, QueryOperator op, TField value)
        {
            return Query().Where(fieldSelector, op, value);
        }

        /// <summary>
        /// Creates a query that sorts the documents by the specified field in ascending order.
        /// </summary>
        public virtual BaseQueryBuilder<T> OrderBy<TField>(Expression<Func<T, TField>> fieldSelector)
        {
            return Query().OrderBy(fieldSelector);
        }

        /// <summary>
        /// Creates a query that sorts the documents by the specified field in descending order.
        /// </summary>
        public virtual BaseQueryBuilder<T> OrderByDescending<TField>(Expression<Func<T, TField>> fieldSelector)
        {
            return Query().OrderByDescending(fieldSelector);
        }

        /// <summary>
        /// Creates a query that limits the number of documents returned.
        /// </summary>
        public virtual BaseQueryBuilder<T> Limit(int limit)
        {
            return Query().Limit(limit);
        }


        /// <summary>
        /// Creates a query that filters documents where the specified field's value is contained in the provided collection.
        /// </summary>
        public virtual BaseQueryBuilder<T> WhereIn<TField>(Expression<Func<T, TField>> fieldSelector, IEnumerable<TField> values)
        {
            return Query().WhereIn(fieldSelector, values);
        }

        /// <summary>
        /// Creates a query that filters documents where the specified field's value is NOT contained in the provided collection.
        /// </summary>
        public virtual BaseQueryBuilder<T> WhereNotIn<TField>(Expression<Func<T, TField>> fieldSelector, IEnumerable<TField> values)
        {
            return Query().WhereNotIn(fieldSelector, values);
        }

        /// <summary>
        /// Creates a query that filters documents where the specified array field contains any of the values in the provided collection.
        /// </summary>
        public virtual BaseQueryBuilder<T> WhereArrayContainsAny<TField>(Expression<Func<T, IEnumerable<TField>>> fieldSelector, IEnumerable<TField> values)
        {
            return Query().WhereArrayContainsAny(fieldSelector, values);
        }

        /// <summary>
        /// Creates a query that starts at the provided document snapshot.
        /// </summary>
        public virtual BaseQueryBuilder<T> StartAt(DocumentSnapshot snapshot)
        {
            return Query().StartAt(snapshot);
        }

        /// <summary>
        /// Creates a query that starts at the provided field values.
        /// </summary>
        public virtual BaseQueryBuilder<T> StartAt(params object[] fieldValues)
        {
            return Query().StartAt(fieldValues);
        }

        /// <summary>
        /// Creates a query that starts after the provided document snapshot.
        /// </summary>
        public virtual BaseQueryBuilder<T> StartAfter(DocumentSnapshot snapshot)
        {
            return Query().StartAfter(snapshot);
        }

        /// <summary>
        /// Creates a query that starts after the provided field values.
        /// </summary>
        public virtual BaseQueryBuilder<T> StartAfter(params object[] fieldValues)
        {
            return Query().StartAfter(fieldValues);
        }

        /// <summary>
        /// Creates a query that ends at the provided document snapshot.
        /// </summary>
        public virtual BaseQueryBuilder<T> EndAt(DocumentSnapshot snapshot)
        {
            return Query().EndAt(snapshot);
        }

        /// <summary>
        /// Creates a query that ends at the provided field values.
        /// </summary>
        public virtual BaseQueryBuilder<T> EndAt(params object[] fieldValues)
        {
            return Query().EndAt(fieldValues);
        }

        /// <summary>
        /// Creates a query that ends before the provided document snapshot.
        /// </summary>
        public virtual BaseQueryBuilder<T> EndBefore(DocumentSnapshot snapshot)
        {
            return Query().EndBefore(snapshot);
        }

        /// <summary>
        /// Creates a query that ends before the provided field values.
        /// </summary>
        public virtual BaseQueryBuilder<T> EndBefore(params object[] fieldValues)
        {
            return Query().EndBefore(fieldValues);
        }

        // --- TODO: Add other query methods (LimitToLast, StartAt, etc.) as needed ---
    }
}