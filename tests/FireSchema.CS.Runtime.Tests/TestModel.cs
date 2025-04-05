using FireSchema.CS.Runtime.Core;
using Google.Cloud.Firestore;
using System.Collections.Generic; // For List<T>

namespace FireSchema.CS.Runtime.Tests
{
    // Define a sample model for testing
    [FirestoreData]
    public class TestModel : IFirestoreDocument
    {
        [FirestoreDocumentId] // Keep short name as Google.Cloud.Firestore is imported
        public string Id { get; set; } = ""; // Initialize to avoid null warnings

        [FirestoreProperty("name")]
        public string Name { get; set; } = "";

        [FirestoreProperty("value")]
        public int Value { get; set; }

        // Property NOT marked with FirestoreProperty, should be ignored by default converter behavior
        public string IgnoredProperty { get; set; } = "should-be-ignored";

        [FirestoreProperty("tags")]
        public List<string> Tags { get; set; } = new List<string>(); // Add a list for array tests
    }
}