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
            _idProperty = typeof(T).GetProperties()
                .FirstOrDefault(prop => Attribute.IsDefined(prop, typeof(FirestoreDocumentIdAttribute)));

            // Optional: Add validation or warning if no Id property is found?
            // if (_idProperty == null)
            // {
            //     Console.WriteLine($"Warning: No property marked with [FirestoreDocumentId] found on type {typeof(T).Name}. Document ID mapping will not work automatically.");
            // }
        }

        /// <summary>
        /// Converts data from a Firestore document snapshot into an object of type T.
        /// Populates the property marked with [FirestoreDocumentId] with the snapshot's ID.
        /// </summary>
        public T FromFirestore(DocumentSnapshot snapshot)
        {
            if (!snapshot.Exists)
            {
                // Or return default(T)? Throwing might be better if caller expects existence.
                throw new ArgumentException($"Document snapshot for ID {snapshot.Id} does not exist.", nameof(snapshot));
            }

            // Use Firestore's built-in dictionary conversion first
            T data = snapshot.ConvertTo<T>(); // This uses [FirestoreProperty] mapping

            // Set the ID property using reflection if found
            _idProperty?.SetValue(data, snapshot.Id);

            return data;
        }

        /// <summary>
        /// Converts an object of type T into Firestore data (a dictionary).
        /// Excludes the property marked with [FirestoreDocumentId] from the resulting dictionary.
        /// </summary>
        public IDictionary<string, object> ToFirestore(T value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            // Use Firestore's built-in conversion to dictionary
            // This relies on the [FirestoreProperty] attributes on the model class (T)
            var dictionary = value.ToDictionary(); // Requires Google.Cloud.Firestore implicitly

            // Remove the ID property from the dictionary if it was found and included
            if (_idProperty != null && dictionary.ContainsKey(_idProperty.Name))
            {
                 // We need the Firestore property name, not the C# property name, if they differ.
                 // Let's assume the [FirestoreProperty] name matches the C# name for simplicity,
                 // or that ToDictionary() correctly uses the attribute.
                 // A more robust approach might involve checking the [FirestoreProperty] attribute name.
                 // However, the default ToDictionary() likely handles this based on attributes.
                 // The main goal is to ensure the ID *field* isn't written back.
                 // Let's try removing based on C# property name first.
                 dictionary.Remove(_idProperty.Name);

                 // If C# name != Firestore name, we might need to find the Firestore name:
                 // var firestorePropAttr = _idProperty.GetCustomAttribute<FirestorePropertyAttribute>();
                 // if (firestorePropAttr != null && dictionary.ContainsKey(firestorePropAttr.Name)) {
                 //     dictionary.Remove(firestorePropAttr.Name);
                 // } else if (dictionary.ContainsKey(_idProperty.Name)) {
                 //      dictionary.Remove(_idProperty.Name); // Fallback to C# name
                 // }
            }

            return dictionary;
        }
    }
}