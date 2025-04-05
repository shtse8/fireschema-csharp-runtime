using Microsoft.VisualStudio.TestTools.UnitTesting;
using FireSchema.CS.Runtime.Core;
using Google.Cloud.Firestore;
using System.Collections.Generic;

namespace FireSchema.CS.Runtime.Tests
{
    // Define a sample model for testing
    [FirestoreData]
    public class TestModel : IFirestoreDocument
    {
        [FirestoreDocumentId]
        public string Id { get; set; } = ""; // Initialize to avoid null warnings

        [FirestoreProperty("name")]
        public string Name { get; set; } = "";

        [FirestoreProperty("value")]
        public int Value { get; set; }

        // Property NOT marked with FirestoreProperty, should be ignored by default converter behavior
        public string IgnoredProperty { get; set; } = "should-be-ignored";
    }

    [TestClass]
    public class FirestoreDataConverterTests
    {
        [TestMethod]
        public void ToFirestore_ShouldExcludeIdProperty()
        {
            // Arrange
            var converter = new FirestoreDataConverter<TestModel>();
            var model = new TestModel
            {
                Id = "test-id-123",
                Name = "Test Name",
                Value = 42,
                IgnoredProperty = "some-other-value" // This should also be excluded by default ToDictionary
            };

            // Act
            var dictionary = converter.ToFirestore(model);

            // Assert
            Assert.IsNotNull(dictionary, "Resulting dictionary should not be null.");
            Assert.IsFalse(dictionary.ContainsKey("Id"), "Dictionary should not contain the 'Id' property.");
            Assert.IsFalse(dictionary.ContainsKey(nameof(TestModel.Id)), "Dictionary should not contain the 'Id' property (checked by C# name)."); // Double check
            Assert.IsTrue(dictionary.ContainsKey("name"), "Dictionary should contain the 'name' property.");
            Assert.AreEqual("Test Name", dictionary["name"]);
            Assert.IsTrue(dictionary.ContainsKey("value"), "Dictionary should contain the 'value' property.");
            Assert.AreEqual(42L, dictionary["value"]); // Firestore SDK often uses Int64 (long) for integers
            Assert.IsFalse(dictionary.ContainsKey("IgnoredProperty"), "Dictionary should not contain properties without [FirestoreProperty].");
        }

        // --- FromFirestore tests require mocking DocumentSnapshot ---
        // TODO: Add tests for FromFirestore using a mocking framework (like Moq)
        //       or by creating mock DocumentSnapshot instances if possible.

        /*
        [TestMethod]
        public void FromFirestore_ShouldPopulateIdAndProperties()
        {
            // Arrange
            var converter = new FirestoreDataConverter<TestModel>();
            var mockSnapshot = CreateMockSnapshot("doc-abc", new Dictionary<string, object>
            {
                { "name", "From Snapshot" },
                { "value", 123 }
                // Note: IgnoredProperty won't be in the snapshot data
            });

            // Act
            var model = converter.FromFirestore(mockSnapshot);

            // Assert
            Assert.IsNotNull(model);
            Assert.AreEqual("doc-abc", model.Id, "Id property should be populated from snapshot ID.");
            Assert.AreEqual("From Snapshot", model.Name, "Name property should be populated.");
            Assert.AreEqual(123, model.Value, "Value property should be populated.");
            // IgnoredProperty should have its default value or be null depending on initialization
            Assert.AreEqual("should-be-ignored", model.IgnoredProperty, "IgnoredProperty should retain default value.");
        }

        // Helper to create a mock DocumentSnapshot (requires a mocking library or complex setup)
        private DocumentSnapshot CreateMockSnapshot(string id, Dictionary<string, object> data)
        {
             // This is highly simplified and likely needs Moq or similar
             // var mock = new Moq.Mock<DocumentSnapshot>();
             // mock.Setup(s => s.Id).Returns(id);
             // mock.Setup(s => s.Exists).Returns(true);
             // mock.Setup(s => s.ToDictionary()).Returns(data);
             // // Need to mock ConvertTo<T> to use the dictionary and handle attributes... complex!
             // // It might be easier to test the converter indirectly via BaseCollectionRef tests
             // // using the Firestore emulator.
             // return mock.Object;
             throw new NotImplementedException("Mocking DocumentSnapshot is complex. Consider integration testing.");
        }
        */
    }
}