using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection; // Required for MemberInfo
using System.Threading.Tasks;
using System.Linq; // For Cast<T>

namespace FireSchema.CS.Runtime.Core
{
    /// <summary>
    /// Base class for generated Firestore update builders.
    /// Provides common functionality for constructing update operations.
    /// </summary>
    /// <typeparam name="T">The type of the document data (must implement IFirestoreDocument).</typeparam>
    public class BaseUpdateBuilder<T> where T : class, IFirestoreDocument, new() // Removed 'abstract'
    {
        protected readonly DocumentReference _docRef;
        protected readonly Dictionary<FieldPath, object> _updates = new Dictionary<FieldPath, object>();

        /// <summary>
        /// Initializes a new instance of the BaseUpdateBuilder class.
        /// </summary>
        /// <param name="docRef">The DocumentReference to update.</param>
        internal BaseUpdateBuilder(DocumentReference docRef) // Changed from protected to internal
        {
            _docRef = docRef ?? throw new ArgumentNullException(nameof(docRef));
        }

        /// <summary>
        /// Adds a field update operation to the builder.
        /// Use FieldValue constants (e.g., FieldValue.Delete, FieldValue.ServerTimestamp) for special operations.
        /// </summary>
        /// <param name="fieldPath">The path to the field to update.</param>
        /// <param name="value">The new value for the field.</param>
        /// <returns>The current update builder instance for chaining.</returns>
        public virtual BaseUpdateBuilder<T> Set(FieldPath fieldPath, object value)
        {
            _updates[fieldPath] = value;
            return this;
        }

        /// <summary>
        /// Adds a field update operation using a string field path.
        /// </summary>
        /// <param name="fieldPath">The dot-separated path to the field to update.</param>
        /// <param name="value">The new value for the field.</param>
        /// <returns>The current update builder instance for chaining.</returns>
        public virtual BaseUpdateBuilder<T> Set(string fieldPath, object value)
        {
            return Set(new FieldPath(fieldPath.Split('.')), value);
        }

        /// <summary>
        /// Adds a field update operation using a type-safe expression.
        /// Relies on FirestorePropertyAttribute to determine the field name.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldSelector">An expression selecting the field to update (e.g., m => m.Name).</param>
        /// <param name="value">The new value for the field.</param>
        /// <returns>The current update builder instance for chaining.</returns>
        public virtual BaseUpdateBuilder<T> Set<TField>(Expression<Func<T, TField>> fieldSelector, TField value)
        {
            var fieldPath = GetFieldPath(fieldSelector);
            _updates[fieldPath] = value!; // Allow null if TField is nullable
            return this;
        }

        /// <summary>
        /// Adds an operation to delete the specified field.
        /// </summary>
        /// <param name="fieldPath">The path to the field to delete.</param>
        /// <returns>The current update builder instance for chaining.</returns>
        public virtual BaseUpdateBuilder<T> Delete(FieldPath fieldPath)
        {
            return Set(fieldPath, FieldValue.Delete);
        }

        /// <summary>
        /// Adds an operation to delete the specified field using a string path.
        /// </summary>
        /// <param name="fieldPath">The dot-separated path to the field to delete.</param>
        /// <returns>The current update builder instance for chaining.</returns>
        public virtual BaseUpdateBuilder<T> Delete(string fieldPath)
        {
            return Set(fieldPath, FieldValue.Delete);
        }

        /// <summary>
        /// Adds an operation to set the specified field to the server timestamp.
        /// </summary>
        /// <param name="fieldPath">The path to the field to update.</param>
        /// <returns>The current update builder instance for chaining.</returns>
        public virtual BaseUpdateBuilder<T> SetServerTimestamp(FieldPath fieldPath)
        {
            return Set(fieldPath, FieldValue.ServerTimestamp);
        }

        /// <summary>
        /// Adds an operation to set the specified field to the server timestamp using a string path.
        /// </summary>
        /// <param name="fieldPath">The dot-separated path to the field to update.</param>
        /// <returns>The current update builder instance for chaining.</returns>
        public virtual BaseUpdateBuilder<T> SetServerTimestamp(string fieldPath)
        {
            return Set(fieldPath, FieldValue.ServerTimestamp);
        }

        /// <summary>
        /// Adds an operation to atomically increment the specified numeric field.
        /// </summary>
        /// <param name="fieldPath">The path to the numeric field to increment.</param>
        /// <param name="value">The value to increment by (can be negative).</param>
        /// <returns>The current update builder instance for chaining.</returns>
        public virtual BaseUpdateBuilder<T> Increment(FieldPath fieldPath, double value)
        {
             return Set(fieldPath, FieldValue.Increment(value));
        }

        /// <summary>
        /// Adds an operation to atomically increment the specified numeric field using a string path.
        /// </summary>
        /// <param name="fieldPath">The dot-separated path to the numeric field to increment.</param>
        /// <param name="value">The value to increment by (can be negative).</param>
        /// <returns>The current update builder instance for chaining.</returns>
        public virtual BaseUpdateBuilder<T> Increment(string fieldPath, double value)
        {
             return Set(fieldPath, FieldValue.Increment(value));
        }

        /// <summary>
        /// Adds an operation to atomically increment the specified numeric field using a type-safe expression.
        /// </summary>
        /// <typeparam name="TField">The type of the numeric field (e.g., int, double).</typeparam>
        /// <param name="fieldSelector">An expression selecting the field to increment.</param>
        /// <param name="value">The value to increment by (can be negative).</param>
        /// <returns>The current update builder instance for chaining.</returns>
        public virtual BaseUpdateBuilder<T> Increment<TField>(Expression<Func<T, TField>> fieldSelector, double value)
            where TField : struct // Basic constraint for numeric types
        {
             var fieldPath = GetFieldPath(fieldSelector);
             return Set(fieldPath, FieldValue.Increment(value));
        }


        /// <summary>
        /// Adds an operation to atomically add elements to an array field, ensuring uniqueness.
        /// </summary>
        /// <param name="fieldPath">The path to the array field.</param>
        /// <param name="values">The elements to add.</param>
        /// <returns>The current update builder instance for chaining.</returns>
        public virtual BaseUpdateBuilder<T> ArrayUnion(FieldPath fieldPath, params object[] values)
        {
             return Set(fieldPath, FieldValue.ArrayUnion(values));
        }

        /// <summary>
        /// Adds an operation to atomically add elements to an array field using a string path.
        /// </summary>
        /// <param name="fieldPath">The dot-separated path to the array field.</param>
        /// <param name="values">The elements to add.</param>
        /// <returns>The current update builder instance for chaining.</returns>
        public virtual BaseUpdateBuilder<T> ArrayUnion(string fieldPath, params object[] values)
        {
             return Set(fieldPath, FieldValue.ArrayUnion(values));
        }

        /// <summary>
        /// Adds an operation to atomically add elements to an array field using a type-safe expression.
        /// </summary>
        /// <typeparam name="TField">The type of the array elements.</typeparam>
        /// <param name="fieldSelector">An expression selecting the array field.</param>
        /// <param name="values">The elements to add.</param>
        /// <returns>The current update builder instance for chaining.</returns>
        public virtual BaseUpdateBuilder<T> ArrayUnion<TField>(Expression<Func<T, IEnumerable<TField>>> fieldSelector, params TField[] values)
        {
             var fieldPath = GetFieldPath(fieldSelector);
             // Need to cast object[] to TField[]? Firestore SDK might handle object[] directly. Let's assume object[] is fine.
             return Set(fieldPath, FieldValue.ArrayUnion(values.Cast<object>().ToArray())); // Cast IEnumerable<TField> (params TField[]) to object[]
        }


        /// <summary>
        /// Adds an operation to atomically remove all instances of the specified elements from an array field.
        /// </summary>
        /// <param name="fieldPath">The path to the array field.</param>
        /// <param name="values">The elements to remove.</param>
        /// <returns>The current update builder instance for chaining.</returns>
        public virtual BaseUpdateBuilder<T> ArrayRemove(FieldPath fieldPath, params object[] values)
        {
             return Set(fieldPath, FieldValue.ArrayRemove(values));
        }

        /// <summary>
        /// Adds an operation to atomically remove all instances of the specified elements from an array field using a string path.
        /// </summary>
        /// <param name="fieldPath">The dot-separated path to the array field.</param>
        /// <param name="values">The elements to remove.</param>
        /// <returns>The current update builder instance for chaining.</returns>
        public virtual BaseUpdateBuilder<T> ArrayRemove(string fieldPath, params object[] values)
        {
             return Set(fieldPath, FieldValue.ArrayRemove(values));
        }

        /// <summary>
        /// Adds an operation to atomically remove all instances of the specified elements from an array field using a type-safe expression.
        /// </summary>
        /// <typeparam name="TField">The type of the array elements.</typeparam>
        /// <param name="fieldSelector">An expression selecting the array field.</param>
        /// <param name="values">The elements to remove.</param>
        /// <returns>The current update builder instance for chaining.</returns>
        public virtual BaseUpdateBuilder<T> ArrayRemove<TField>(Expression<Func<T, IEnumerable<TField>>> fieldSelector, params TField[] values)
        {
             var fieldPath = GetFieldPath(fieldSelector);
             return Set(fieldPath, FieldValue.ArrayRemove(values.Cast<object>().ToArray())); // Cast IEnumerable<TField> (params TField[]) to object[]
        }


        /// <summary>
        /// Asynchronously applies the accumulated update operations to the document.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the WriteResult.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no update operations have been added.</exception>
        public virtual async Task<WriteResult> ApplyAsync()
        {
            if (_updates.Count == 0)
            {
                throw new InvalidOperationException("No update operations specified.");
            }
            return await _docRef.UpdateAsync(_updates);
        }

        /// <summary>
        /// Extracts the Firestore field path from a member expression.
        /// Relies on FirestorePropertyAttribute.
        /// </summary>
        protected FieldPath GetFieldPath<TField>(Expression<Func<T, TField>> fieldSelector)
        {
            if (!(fieldSelector.Body is MemberExpression memberExpression))
            {
                throw new ArgumentException("Selector must be a MemberExpression.", nameof(fieldSelector));
            }

            var member = memberExpression.Member;
            // Correct namespace for FirestorePropertyAttribute
            var firestorePropertyAttribute = member.GetCustomAttribute<Google.Cloud.Firestore.FirestorePropertyAttribute>();

            if (firestorePropertyAttribute == null || string.IsNullOrEmpty(firestorePropertyAttribute.Name))
            {
                // Fallback to member name if attribute is missing or has no name (though it should have one)
                // Consider throwing an exception if strict attribute usage is required.
                 return new FieldPath(member.Name);
                // throw new InvalidOperationException($"Member '{member.Name}' must have a FirestorePropertyAttribute with a non-empty Name.");
            }

            return new FieldPath(firestorePropertyAttribute.Name);
        }
    } // End of BaseUpdateBuilder<T> class
} // End of namespace