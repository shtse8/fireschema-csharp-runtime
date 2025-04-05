using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions; // For potential future use with strongly-typed Where clauses
using static Google.Cloud.Firestore.V1.StructuredQuery.Types.FieldFilter.Types; // For Operator enum
using System.Threading.Tasks;

namespace FireSchema.CS.Runtime.Core
{
    /// <summary>
    /// Base class for generated Firestore query builders.
    /// Provides common functionality for constructing and executing queries.
    /// </summary>
    /// <typeparam name="T">The type of the document data (must implement IFirestoreDocument).</typeparam>
    public abstract class BaseQueryBuilder<T> where T : class, IFirestoreDocument, new()
    {
        /// <summary>
        /// Gets the underlying Firestore Query object.
        /// </summary>
        public Query FirestoreQuery { get; protected set; } // Use protected set to allow modification by derived classes or internal methods

        /// <summary>
        /// Initializes a new instance of the BaseQueryBuilder class.
        /// </summary>
        /// <param name="query">The initial Firestore query.</param>
        protected BaseQueryBuilder(Query query)
        {
            FirestoreQuery = query ?? throw new ArgumentNullException(nameof(query));
            // Apply the converter to the initial query
            // FirestoreQuery = FirestoreQuery.WithConverter(new FirestoreDataConverter<T>()); // Remove WithConverter
        }

        // Remove the generic Where and WhereExists methods

        // --- Specific Where Methods ---

        /// <summary>
        /// Creates a new query that filters the documents where the specified field is equal to the specified value.
        /// </summary>
        public virtual BaseQueryBuilder<T> WhereEqualTo(string fieldPath, object value)
        {
            FirestoreQuery = FirestoreQuery.WhereEqualTo(fieldPath, value);
            return this; // Mutable approach
        }

        /// <summary>
        /// Creates a new query that filters the documents where the specified field is not equal to the specified value.
        /// </summary>
        public virtual BaseQueryBuilder<T> WhereNotEqualTo(string fieldPath, object value)
        {
            FirestoreQuery = FirestoreQuery.WhereNotEqualTo(fieldPath, value);
            return this;
        }

        /// <summary>
        /// Creates a new query that filters the documents where the specified field is less than the specified value.
        /// </summary>
        public virtual BaseQueryBuilder<T> WhereLessThan(string fieldPath, object value)
        {
             FirestoreQuery = FirestoreQuery.WhereLessThan(fieldPath, value);
             return this;
        }

         /// <summary>
        /// Creates a new query that filters the documents where the specified field is less than or equal to the specified value.
        /// </summary>
        public virtual BaseQueryBuilder<T> WhereLessThanOrEqualTo(string fieldPath, object value)
        {
             FirestoreQuery = FirestoreQuery.WhereLessThanOrEqualTo(fieldPath, value);
             return this;
        }

        /// <summary>
        /// Creates a new query that filters the documents where the specified field is greater than the specified value.
        /// </summary>
        public virtual BaseQueryBuilder<T> WhereGreaterThan(string fieldPath, object value)
        {
             FirestoreQuery = FirestoreQuery.WhereGreaterThan(fieldPath, value);
             return this;
        }

        /// <summary>
        /// Creates a new query that filters the documents where the specified field is greater than or equal to the specified value.
        /// </summary>
        public virtual BaseQueryBuilder<T> WhereGreaterThanOrEqualTo(string fieldPath, object value)
        {
             FirestoreQuery = FirestoreQuery.WhereGreaterThanOrEqualTo(fieldPath, value);
             return this;
        }

        /// <summary>
        /// Creates a new query that filters the documents where the specified field is contained in the specified array.
        /// </summary>
        public virtual BaseQueryBuilder<T> WhereIn(string fieldPath, IEnumerable<object> values)
        {
             FirestoreQuery = FirestoreQuery.WhereIn(fieldPath, values);
             return this;
        }

        /// <summary>
        /// Creates a new query that filters the documents where the specified field is not contained in the specified array.
        /// </summary>
        public virtual BaseQueryBuilder<T> WhereNotIn(string fieldPath, IEnumerable<object> values)
        {
             FirestoreQuery = FirestoreQuery.WhereNotIn(fieldPath, values);
             return this;
        }

        /// <summary>
        /// Creates a new query that filters the documents where the specified array field contains the specified value.
        /// </summary>
        public virtual BaseQueryBuilder<T> WhereArrayContains(string fieldPath, object value)
        {
             FirestoreQuery = FirestoreQuery.WhereArrayContains(fieldPath, value);
             return this;
        }

         /// <summary>
        /// Creates a new query that filters the documents where the specified array field contains any of the specified values.
        /// </summary>
        public virtual BaseQueryBuilder<T> WhereArrayContainsAny(string fieldPath, IEnumerable<object> values)
        {
             FirestoreQuery = FirestoreQuery.WhereArrayContainsAny(fieldPath, values);
             return this;
        }

        // --- TODO: Add strongly-typed Where methods using Expressions ---
        // public virtual BaseQueryBuilder<T> WhereEqualTo<TField>(Expression<Func<T, TField>> fieldSelector, TField value) { ... }


        /// <summary>
        /// Creates a new query that orders the documents by the specified field in ascending order.
        /// </summary>
        /// <param name="fieldPath">The path to the field to order by.</param>
        /// <returns>A new query builder with the added ordering.</returns>
        public virtual BaseQueryBuilder<T> OrderByAscending(string fieldPath)
        {
            FirestoreQuery = FirestoreQuery.OrderBy(fieldPath); // Use OrderBy for ascending
            return this; // Mutable approach
        }

        /// <summary>
        /// Creates a new query that orders the documents by the specified field in descending order.
        /// </summary>
        /// <param name="fieldPath">The path to the field to order by.</param>
        /// <returns>A new query builder with the added ordering.</returns>
        public virtual BaseQueryBuilder<T> OrderByDescending(string fieldPath)
        {
            FirestoreQuery = FirestoreQuery.OrderByDescending(fieldPath); // Use OrderByDescending
            return this; // Mutable approach
        }

        /// <summary>
        /// Creates a new query that limits the number of documents returned.
        /// </summary>
        /// <param name="limit">The maximum number of documents to return.</param>
        /// <returns>A new query builder with the added limit.</returns>
        public virtual BaseQueryBuilder<T> Limit(int limit)
        {
            FirestoreQuery = FirestoreQuery.Limit(limit); // Remove WithConverter
            return this; // Mutable approach
        }

         /// <summary>
        /// Creates a new query that limits the number of documents returned from the end of the ordered results.
        /// </summary>
        /// <param name="limit">The maximum number of documents to return.</param>
        /// <returns>A new query builder with the added limit.</returns>
        public virtual BaseQueryBuilder<T> LimitToLast(int limit)
        {
            FirestoreQuery = FirestoreQuery.LimitToLast(limit); // Remove WithConverter
            return this; // Mutable approach
        }

        // --- TODO: Add pagination methods (StartAt, StartAfter, EndAt, EndBefore) ---


        /// <summary>
        /// Asynchronously executes the query and returns the results as a QuerySnapshot.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the query snapshot.</returns>
        public virtual async Task<QuerySnapshot> GetSnapshotAsync()
        {
            return await FirestoreQuery.GetSnapshotAsync();
        }

        /// <summary>
        /// Asynchronously executes the query and returns the results as a list of document data objects.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of documents.</returns>
        // Instance of the converter for manual use
        private static readonly FirestoreDataConverter<T> _converter = new FirestoreDataConverter<T>();

        public virtual async Task<List<T>> GetAsync()
        {
            var snapshot = await GetSnapshotAsync();
            // Manually convert each document using the internal converter method
            return snapshot.Documents.Select(doc => _converter.FromFirestore(doc)).ToList();
        }

        // --- TODO: Add Listen/Snapshot methods for real-time updates ---

    }
}