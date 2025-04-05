// No need to import FireSchema.CS.Runtime.Core for the attribute anymore
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection; // Required for reflection

namespace FireSchema.CS.Runtime.Core
{
    /// <summary>
    /// Generic Firestore converter for types implementing IFirestoreDocument.
    /// Automatically handles mapping the document ID to the property marked with [FirestoreDocumentId].
    /// </summary>
    /// <typeparam name="T">The type of the document data (must implement IFirestoreDocument).</typeparam>
    public class FirestoreDataConverter<T> : IFirestoreConverter<T> where T : class, IFirestoreDocument, new()
    {
        private readonly PropertyInfo? _idProperty;

        public FirestoreDataConverter()
        {
            // Find the property marked with [FirestoreDocumentId] using reflection
            // Use the official attribute from Google.Cloud.Firestore
            _idProperty = typeof(T).GetProperties()
                .FirstOrDefault(prop => Attribute.IsDefined(prop, typeof(Google.Cloud.Firestore.FirestoreDocumentIdAttribute)));

            // Optional: Add validation or warning if no Id property is found?
            // if (_idProperty == null)
            // {
            //     Console.WriteLine($"Warning: No property marked with [FirestoreDocumentId] found on type {typeof(T).Name}. Document ID mapping will not work automatically.");
            // }
        }

        /// <summary>
        /// Converts data from Firestore (typically a Dictionary<string, object>) into an object of type T.
        /// This method is called by Firestore infrastructure, often after fetching data.
        /// The 'value' parameter here is usually the dictionary from the snapshot.
        /// </summary>
        // Explicit interface implementation for IFirestoreConverter<T>.FromFirestore(object)
        // This is required by the interface but likely not the primary path used by Firestore
        // when ConvertTo<T> is called on a snapshot with this converter applied.
        T IFirestoreConverter<T>.FromFirestore(object value)
        {
             if (value == null) return default(T)!;

             // We don't have the snapshot ID here.
             // If this method *is* called by Firestore for object mapping,
             // it means the automatic mapping via ConvertTo<T> isn't happening as expected
             // when the converter is applied via WithConverter.
             // Throwing here makes it clear this path isn't supported for complex objects.
             if (value is IDictionary<string, object>)
             {
                 throw new NotImplementedException($"Direct conversion from IDictionary<string, object> in {nameof(IFirestoreConverter<T>.FromFirestore)} is not supported by this converter. Use snapshot.ConvertTo<T>().");
             }

             // Attempt conversion only for simple/primitive types if Firestore stores them directly
             try {
                 if (!typeof(T).IsValueType || Nullable.GetUnderlyingType(typeof(T)) != null) {
                    if (value == null) return default(T)!;
                 }
                 return (T)Convert.ChangeType(value, typeof(T));
             } catch (Exception ex) {
                 throw new ArgumentException($"Cannot convert Firestore value of type {value?.GetType().Name ?? "null"} to {typeof(T).Name}. This converter primarily handles object-to-dictionary conversion.", nameof(value), ex);
             }
        }

        /// <summary>
        /// Converts an object of type T into Firestore data (typically a Dictionary<string, object>).
        /// This method is called by Firestore infrastructure before writing data.
        /// </summary>
        /// <returns>An object representing the data to be stored (usually a dictionary).</returns>
        // This is the public method we'll call directly and also use for the interface.
        /// <summary>
        /// Converts an object of type T into Firestore data (a dictionary).
        /// Excludes the property marked with [FirestoreDocumentId] from the resulting dictionary.
        /// </summary>
        public IDictionary<string, object> ToFirestore(T value)
        {
             if (value == null) throw new ArgumentNullException(nameof(value));

             // Manually create a dictionary from properties marked with FirestoreProperty
             var dictionary = new Dictionary<string, object>();
             var properties = typeof(T).GetProperties();

             foreach (var prop in properties)
             {
                 // Skip the ID property marked with FirestoreDocumentId
                 if (_idProperty != null && prop.Name == _idProperty.Name)
                 {
                     continue;
                 }

                 // Only include properties marked with FirestoreProperty
                 var firestorePropAttr = prop.GetCustomAttribute<FirestorePropertyAttribute>();
                 if (firestorePropAttr != null)
                 {
                     string firestoreFieldName = firestorePropAttr.Name ?? prop.Name;
                     object? propValue = prop.GetValue(value);
                     if (propValue != null)
                     {
                          dictionary[firestoreFieldName] = propValue;
                     }
                     // TODO: Handle null values according to Firestore settings if needed
                 }
             }
             return dictionary;
        }

        // Explicit interface implementation for IFirestoreConverter<T>.ToFirestore(T) returning object
        object IFirestoreConverter<T>.ToFirestore(T value)
        {
            // Delegate to our public implementation
            return ToFirestore(value);
        }
        // Keep the original FromFirestore(DocumentSnapshot) method for internal use by BaseCollectionRef
        // This method correctly handles the ID property.
        /// <summary>
        /// Converts data from a Firestore document snapshot into an object of type T.
        /// Populates the property marked with [FirestoreDocumentId] with the snapshot's ID.
        /// This is intended for use after fetching a snapshot.
        /// </summary>
        public T FromFirestore(DocumentSnapshot snapshot) // Make public for test access
        {
            if (!snapshot.Exists)
            {
                // Return default or throw? Let's return default for flexibility.
                // Caller (like BaseCollectionRef.GetAsync) should handle null check.
                return default(T)!;
                // throw new ArgumentException($"Document snapshot for ID {snapshot.Id} does not exist.", nameof(snapshot));
            }
            T data = snapshot.ConvertTo<T>(); // Uses [FirestoreProperty] mapping
            _idProperty?.SetValue(data, snapshot.Id); // Set ID
            return data;
        }
    }
}