using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FireSchema.CS.Runtime.Core
{
    /// <summary>
    /// Base class for generated Firestore update builders.
    /// Provides common functionality for constructing update operations.
    /// </summary>
    /// <typeparam name="T">The type of the document data (must implement IFirestoreDocument).</typeparam>
    public abstract class BaseUpdateBuilder<T> where T : class, IFirestoreDocument, new()
    {
        protected readonly DocumentReference _docRef;
        protected readonly Dictionary<FieldPath, object> _updates = new Dictionary<FieldPath, object>();

        /// <summary>
        /// Initializes a new instance of the BaseUpdateBuilder class.
        /// </summary>
        /// <param name="docRef">The DocumentReference to update.</param>
        protected BaseUpdateBuilder(DocumentReference docRef)
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

        // --- TODO: Add Increment, ArrayUnion, ArrayRemove methods ---
        // public virtual BaseUpdateBuilder<T> Increment(FieldPath fieldPath, double value) => Set(fieldPath, FieldValue.Increment(value));
        // public virtual BaseUpdateBuilder<T> ArrayUnion(FieldPath fieldPath, params object[] values) => Set(fieldPath, FieldValue.ArrayUnion(values));
        // public virtual BaseUpdateBuilder<T> ArrayRemove(FieldPath fieldPath, params object[] values) => Set(fieldPath, FieldValue.ArrayRemove(values));


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
    }
}