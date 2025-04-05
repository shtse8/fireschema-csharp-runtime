using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq; // Required for Any() and Count() on IEnumerable
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace FireSchema.CS.Runtime.Core
{
    /// <summary>
    /// Base class for generated Firestore query builders.
    /// Provides methods for constructing and executing queries in a type-safe manner.
    /// </summary>
    /// <typeparam name="T">The type of the document data (must implement IFirestoreDocument).</typeparam>
    public class BaseQueryBuilder<T> where T : class, IFirestoreDocument, new()
    {
        protected Query _query;
        private static readonly FirestoreDataConverter<T> _converter = new FirestoreDataConverter<T>();

        /// <summary>
        /// Initializes a new instance of the BaseQueryBuilder class.
        /// </summary>
        /// <param name="initialQuery">The initial Firestore Query object (usually starting from a CollectionReference).</param>
        internal BaseQueryBuilder(Query initialQuery)
        {
            _query = initialQuery ?? throw new ArgumentNullException(nameof(initialQuery));
        }

        /// <summary>
        /// Creates a new query that filters the documents based on a specified field, operator, and value.
        /// Uses type-safe expression to determine the field path.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldSelector">An expression selecting the field to filter (e.g., m => m.Name).</param>
        /// <param name="op">The query operator (e.g., QueryOperator.EqualTo).</param>
        /// <param name="value">The value to compare against.</param>
        /// <returns>A new query builder instance with the filter applied.</returns>
        public virtual BaseQueryBuilder<T> Where<TField>(Expression<Func<T, TField>> fieldSelector, QueryOperator op, TField value)
        {
            var fieldPath = GetFieldPath(fieldSelector);
            Query newQuery;
            switch (op)
            {
                case QueryOperator.EqualTo:
                    newQuery = _query.WhereEqualTo(fieldPath, value);
                    break;
                case QueryOperator.NotEqualTo:
                    newQuery = _query.WhereNotEqualTo(fieldPath, value);
                    break;
                case QueryOperator.LessThan:
                    newQuery = _query.WhereLessThan(fieldPath, value);
                    break;
                case QueryOperator.LessThanOrEqualTo:
                    newQuery = _query.WhereLessThanOrEqualTo(fieldPath, value);
                    break;
                case QueryOperator.GreaterThan:
                    newQuery = _query.WhereGreaterThan(fieldPath, value);
                    break;
                case QueryOperator.GreaterThanOrEqualTo:
                    newQuery = _query.WhereGreaterThanOrEqualTo(fieldPath, value);
                    break;
                case QueryOperator.ArrayContains:
                    newQuery = _query.WhereArrayContains(fieldPath, value);
                    break;
                // Add cases for In, NotIn, ArrayContainsAny as needed, they might require IEnumerable<TField>
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), $"Unsupported query operator: {op}");
            }
            return new BaseQueryBuilder<T>(newQuery);
        }

        /// <summary>
        /// Creates a new query that filters documents where the specified field's value is contained in the provided collection.
        /// </summary>
        public virtual BaseQueryBuilder<T> WhereIn<TField>(Expression<Func<T, TField>> fieldSelector, IEnumerable<TField> values)
        {
            var fieldPath = GetFieldPath(fieldSelector);
            // Firestore SDK requires the value collection to be non-empty and contain <= 30 elements.
            if (values == null || !values.Any())
            {
                throw new ArgumentException("Value collection for 'In' query cannot be null or empty.", nameof(values));
            }
            if (values.Count() > 30)
            {
                // Consider splitting into multiple queries if this limit is hit often,
                // but the base library will throw for now to match SDK behavior.
                throw new ArgumentException("Value collection for 'In' query cannot contain more than 30 elements.", nameof(values));
            }
            var newQuery = _query.WhereIn(fieldPath, values);
            return new BaseQueryBuilder<T>(newQuery);
        }

        /// <summary>
        /// Creates a new query that filters documents where the specified field's value is NOT contained in the provided collection.
        /// </summary>
        public virtual BaseQueryBuilder<T> WhereNotIn<TField>(Expression<Func<T, TField>> fieldSelector, IEnumerable<TField> values)
        {
            var fieldPath = GetFieldPath(fieldSelector);
            // Firestore SDK requires the value collection to be non-empty and contain <= 30 elements.
            if (values == null || !values.Any())
            {
                throw new ArgumentException("Value collection for 'Not In' query cannot be null or empty.", nameof(values));
            }
            if (values.Count() > 30)
            {
                throw new ArgumentException("Value collection for 'Not In' query cannot contain more than 30 elements.", nameof(values));
            }
            var newQuery = _query.WhereNotIn(fieldPath, values);
            return new BaseQueryBuilder<T>(newQuery);
        }

        /// <summary>
        /// Creates a new query that filters documents where the specified array field contains any of the values in the provided collection.
        /// </summary>
        public virtual BaseQueryBuilder<T> WhereArrayContainsAny<TField>(Expression<Func<T, IEnumerable<TField>>> fieldSelector, IEnumerable<TField> values) // Field must be IEnumerable
        {
            var fieldPath = GetFieldPath(fieldSelector);
            // Firestore SDK requires the value collection to be non-empty and contain <= 30 elements.
            if (values == null || !values.Any())
            {
                throw new ArgumentException("Value collection for 'Array Contains Any' query cannot be null or empty.", nameof(values));
            }
            if (values.Count() > 30)
            {
                throw new ArgumentException("Value collection for 'Array Contains Any' query cannot contain more than 30 elements.", nameof(values));
            }
            var newQuery = _query.WhereArrayContainsAny(fieldPath, values);
            return new BaseQueryBuilder<T>(newQuery);
        }


        /// <summary>
        /// Creates a new query that sorts the documents by the specified field.
        /// Uses type-safe expression to determine the field path.
        /// </summary>
        /// <param name="fieldSelector">An expression selecting the field to sort by.</param>
        /// <param name="fieldSelector">An expression selecting the field to sort by (ascending).</param>
        /// <returns>A new query builder instance with the ascending ordering applied.</returns>
        public virtual BaseQueryBuilder<T> OrderBy<TField>(Expression<Func<T, TField>> fieldSelector)
        {
            var fieldPath = GetFieldPath(fieldSelector);
            var newQuery = _query.OrderBy(fieldPath); // Ascending by default
            return new BaseQueryBuilder<T>(newQuery);
        }

        /// <summary>
        /// Creates a new query that sorts the documents by the specified field in descending order.
        /// Uses type-safe expression to determine the field path.
        /// </summary>
        /// <param name="fieldSelector">An expression selecting the field to sort by (descending).</param>
        /// <returns>A new query builder instance with the descending ordering applied.</returns>
        public virtual BaseQueryBuilder<T> OrderByDescending<TField>(Expression<Func<T, TField>> fieldSelector)
        {
            var fieldPath = GetFieldPath(fieldSelector);
            var newQuery = _query.OrderByDescending(fieldPath);
            return new BaseQueryBuilder<T>(newQuery);
        }

        /// <summary>
        /// Creates a new query that limits the number of documents returned.
        /// </summary>
        /// <param name="limit">The maximum number of documents to return.</param>
        /// <returns>A new query builder instance with the limit applied.</returns>
        public virtual BaseQueryBuilder<T> Limit(int limit)
        {
            var newQuery = _query.Limit(limit);
            return new BaseQueryBuilder<T>(newQuery);
        }

        /// <summary>
        /// Creates a new query that limits the number of documents returned from the end of the ordered results.
        /// Requires a preceding OrderBy clause.
        /// </summary>
        /// <param name="limit">The maximum number of documents to return from the end.</param>
        /// <returns>A new query builder instance with the limit applied.</returns>
        public virtual BaseQueryBuilder<T> LimitToLast(int limit)
        {
             var newQuery = _query.LimitToLast(limit);
             return new BaseQueryBuilder<T>(newQuery);
        }


        /// <summary>
        /// Creates a new query that starts at the provided document snapshot.
        /// Requires a preceding OrderBy clause that matches the snapshot's ordering.
        /// </summary>
        /// <param name="snapshot">The snapshot of the document to start at.</param>
        /// <returns>A new query builder instance with the starting point applied.</returns>
        public virtual BaseQueryBuilder<T> StartAt(DocumentSnapshot snapshot)
        {
            var newQuery = _query.StartAt(snapshot);
            return new BaseQueryBuilder<T>(newQuery);
        }

        /// <summary>
        /// Creates a new query that starts at the provided field values.
        /// Requires a preceding OrderBy clause that matches the number and order of the field values.
        /// </summary>
        /// <param name="fieldValues">The field values to start this query at, in order of the OrderBy clauses.</param>
        /// <returns>A new query builder instance with the starting point applied.</returns>
        public virtual BaseQueryBuilder<T> StartAt(params object[] fieldValues)
        {
            var newQuery = _query.StartAt(fieldValues);
            return new BaseQueryBuilder<T>(newQuery);
        }

        /// <summary>
        /// Creates a new query that starts after the provided document snapshot.
        /// Requires a preceding OrderBy clause that matches the snapshot's ordering.
        /// </summary>
        /// <param name="snapshot">The snapshot of the document to start after.</param>
        /// <returns>A new query builder instance with the starting point applied.</returns>
        public virtual BaseQueryBuilder<T> StartAfter(DocumentSnapshot snapshot)
        {
            var newQuery = _query.StartAfter(snapshot);
            return new BaseQueryBuilder<T>(newQuery);
        }

        /// <summary>
        /// Creates a new query that starts after the provided field values.
        /// Requires a preceding OrderBy clause that matches the number and order of the field values.
        /// </summary>
        /// <param name="fieldValues">The field values to start this query after, in order of the OrderBy clauses.</param>
        /// <returns>A new query builder instance with the starting point applied.</returns>
        public virtual BaseQueryBuilder<T> StartAfter(params object[] fieldValues)
        {
            var newQuery = _query.StartAfter(fieldValues);
            return new BaseQueryBuilder<T>(newQuery);
        }

        /// <summary>
        /// Creates a new query that ends at the provided document snapshot.
        /// Requires a preceding OrderBy clause that matches the snapshot's ordering.
        /// </summary>
        /// <param name="snapshot">The snapshot of the document to end at.</param>
        /// <returns>A new query builder instance with the ending point applied.</returns>
        public virtual BaseQueryBuilder<T> EndAt(DocumentSnapshot snapshot)
        {
            var newQuery = _query.EndAt(snapshot);
            return new BaseQueryBuilder<T>(newQuery);
        }

        /// <summary>
        /// Creates a new query that ends at the provided field values.
        /// Requires a preceding OrderBy clause that matches the number and order of the field values.
        /// </summary>
        /// <param name="fieldValues">The field values to end this query at, in order of the OrderBy clauses.</param>
        /// <returns>A new query builder instance with the ending point applied.</returns>
        public virtual BaseQueryBuilder<T> EndAt(params object[] fieldValues)
        {
            var newQuery = _query.EndAt(fieldValues);
            return new BaseQueryBuilder<T>(newQuery);
        }

        /// <summary>
        /// Creates a new query that ends before the provided document snapshot.
        /// Requires a preceding OrderBy clause that matches the snapshot's ordering.
        /// </summary>
        /// <param name="snapshot">The snapshot of the document to end before.</param>
        /// <returns>A new query builder instance with the ending point applied.</returns>
        public virtual BaseQueryBuilder<T> EndBefore(DocumentSnapshot snapshot)
        {
            var newQuery = _query.EndBefore(snapshot);
            return new BaseQueryBuilder<T>(newQuery);
        }

        /// <summary>
        /// Creates a new query that ends before the provided field values.
        /// Requires a preceding OrderBy clause that matches the number and order of the field values.
        /// </summary>
        /// <param name="fieldValues">The field values to end this query before, in order of the OrderBy clauses.</param>
        /// <returns>A new query builder instance with the ending point applied.</returns>
        public virtual BaseQueryBuilder<T> EndBefore(params object[] fieldValues)
        {
            var newQuery = _query.EndBefore(fieldValues);
            return new BaseQueryBuilder<T>(newQuery);
        }

        /// <summary>
        /// Asynchronously executes the query and returns a QuerySnapshot.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the QuerySnapshot.</returns>
        public virtual async Task<QuerySnapshot> GetSnapshotAsync()
        {
            return await _query.GetSnapshotAsync();
        }

        /// <summary>
        /// Asynchronously executes the query and returns the results as a list of T.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of documents.</returns>
        public virtual async Task<List<T>> GetAsync()
        {
            var snapshot = await GetSnapshotAsync();
            var results = new List<T>();
            foreach (var document in snapshot.Documents)
            {
                var item = _converter.FromFirestore(document);
                if (item != null) // Converter handles non-existent docs returning null
                {
                    results.Add(item);
                }
            }
            return results;
        }


        /// <summary>
        /// Extracts the Firestore field path from a member expression.
        /// Relies on FirestorePropertyAttribute if present, otherwise uses the member name.
        /// Handles nested properties (e.g., m => m.Address.Street).
        /// </summary>
        protected FieldPath GetFieldPath<TField>(Expression<Func<T, TField>> fieldSelector)
        {
            var pathSegments = new List<string>();
            Expression currentExpression = fieldSelector.Body;

            while (currentExpression is MemberExpression memberExpression)
            {
                var member = memberExpression.Member;
                var firestorePropertyAttribute = member.GetCustomAttribute<FirestorePropertyAttribute>();
                var segmentName = (firestorePropertyAttribute?.Name) ?? member.Name;
                pathSegments.Insert(0, segmentName); // Prepend to build path correctly
                currentExpression = memberExpression.Expression;
            }

            if (pathSegments.Count == 0)
            {
                // This might happen for non-member expressions, e.g., constants or method calls
                throw new ArgumentException("Selector must be a chain of MemberExpressions.", nameof(fieldSelector));
            }

            return new FieldPath(pathSegments.ToArray());
        }
    }

    /// <summary>
    /// Represents the available query operators.
    /// Mirrors Firestore operators for clarity.
    /// </summary>
    public enum QueryOperator
    {
        LessThan,
        LessThanOrEqualTo,
        EqualTo,
        GreaterThan,
        GreaterThanOrEqualTo,
        NotEqualTo,
        ArrayContains,
        In,
        NotIn,
        ArrayContainsAny
    }
}