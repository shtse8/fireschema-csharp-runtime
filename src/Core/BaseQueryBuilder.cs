using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions; // For potential future use with strongly-typed Where clauses
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
            FirestoreQuery = FirestoreQuery.WithConverter(new FirestoreDataConverter<T>());
        }

        /// <summary>
        /// Creates a new query that filters the documents based on the specified field, operator, and value.
        /// </summary>
        /// <param name="fieldPath">The path to the field to filter on.</param>
        /// <param name="op">The comparison operator.</param>
        /// <param name="value">The value to compare against.</param>
        /// <returns>A new query builder with the added filter.</returns>
        public virtual BaseQueryBuilder<T> Where(string fieldPath, QueryOperator op, object value)
        {
            Query newQuery = FirestoreQuery.Where(fieldPath, op, value);
            // Return a new instance of the *derived* builder type if possible,
            // otherwise return a new BaseQueryBuilder. This requires the derived class
            // to override and return its own type. For simplicity here, we might return BaseQueryBuilder
            // or require derived classes to handle chaining properly.
            // Let's return 'this' after modifying the query for now, assuming mutation is acceptable for the builder pattern here.
            // A better approach is immutable builders returning new instances.
             FirestoreQuery = newQuery.WithConverter(new FirestoreDataConverter<T>()); // Re-apply converter
             return this; // Or return new DerivedBuilder(newQuery);
            // Let's try immutable:
            // return new BaseQueryBuilder<T>(newQuery); // This loses the derived type. Needs abstract factory or similar.
            // For now, let's stick to the mutable approach for simplicity in the base class,
            // derived classes can override for true immutability if needed.
        }

         /// <summary>
        /// Creates a new query that filters the documents where the specified field exists.
        /// </summary>
        /// <param name="fieldPath">The path to the field to filter on.</param>
        /// <returns>A new query builder with the added filter.</returns>
        public virtual BaseQueryBuilder<T> WhereExists(string fieldPath)
        {
             // Firestore doesn't have a direct 'exists' operator.
             // Common workaround: check if the field is not equal to null.
             // This might not cover all cases perfectly depending on the data.
             // Another approach is comparing with null AND comparing with "" for strings etc.
             // Let's use the null check for now.
             return Where(fieldPath, QueryOperator.NotEqual, null);
        }


        // --- TODO: Add strongly-typed Where methods using Expressions ---
        // public virtual BaseQueryBuilder<T> Where<TField>(Expression<Func<T, TField>> fieldSelector, QueryOperator op, TField value) { ... }


        /// <summary>
        /// Creates a new query that orders the documents by the specified field.
        /// </summary>
        /// <param name="fieldPath">The path to the field to order by.</param>
        /// <param name="direction">The direction to order by (Ascending or Descending).</param>
        /// <returns>A new query builder with the added ordering.</returns>
        public virtual BaseQueryBuilder<T> OrderBy(string fieldPath, QueryDirection direction)
        {
            FirestoreQuery = FirestoreQuery.OrderBy(fieldPath, direction).WithConverter(new FirestoreDataConverter<T>());
            return this; // Mutable approach
        }

        /// <summary>
        /// Creates a new query that limits the number of documents returned.
        /// </summary>
        /// <param name="limit">The maximum number of documents to return.</param>
        /// <returns>A new query builder with the added limit.</returns>
        public virtual BaseQueryBuilder<T> Limit(int limit)
        {
            FirestoreQuery = FirestoreQuery.Limit(limit).WithConverter(new FirestoreDataConverter<T>());
            return this; // Mutable approach
        }

         /// <summary>
        /// Creates a new query that limits the number of documents returned from the end of the ordered results.
        /// </summary>
        /// <param name="limit">The maximum number of documents to return.</param>
        /// <returns>A new query builder with the added limit.</returns>
        public virtual BaseQueryBuilder<T> LimitToLast(int limit)
        {
            FirestoreQuery = FirestoreQuery.LimitToLast(limit).WithConverter(new FirestoreDataConverter<T>());
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
        public virtual async Task<List<T>> GetAsync()
        {
            var snapshot = await GetSnapshotAsync();
            // The converter applied to the query handles the conversion for each document
            return snapshot.Documents.Select(doc => doc.ConvertTo<T>()).ToList();
        }

        // --- TODO: Add Listen/Snapshot methods for real-time updates ---

    }
}