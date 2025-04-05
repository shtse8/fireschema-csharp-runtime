using NUnit.Framework; // Use NUnit framework
using FireSchema.CS.Runtime.Core;
using Google.Cloud.Firestore; // Ensure Firestore is imported
using System.Collections.Generic;

namespace FireSchema.CS.Runtime.Tests
{
    // TestModel definition moved to TestModel.cs
    [TestFixture] // NUnit attribute for test classes
    public class FirestoreDataConverterTests
    {
        [Test] // NUnit attribute for test methods
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
            // Use NUnit's constraint-based assertion model
            Assert.That(dictionary, Is.Not.Null, "Resulting dictionary should not be null.");
            Assert.That(dictionary.ContainsKey("Id"), Is.False, "Dictionary should not contain the 'Id' property.");
            Assert.That(dictionary.ContainsKey(nameof(TestModel.Id)), Is.False, "Dictionary should not contain the 'Id' property (checked by C# name)."); // Double check
            Assert.That(dictionary.ContainsKey("name"), Is.True, "Dictionary should contain the 'name' property.");
            Assert.That(dictionary["name"], Is.EqualTo("Test Name"));
            Assert.That(dictionary.ContainsKey("value"), Is.True, "Dictionary should contain the 'value' property.");
            Assert.That(dictionary["value"], Is.EqualTo(42L)); // Firestore SDK often uses Int64 (long) for integers
            Assert.That(dictionary.ContainsKey("IgnoredProperty"), Is.False, "Dictionary should not contain properties without [FirestoreProperty].");
        }

        // --- FromFirestore tests require mocking DocumentSnapshot ---
        // TODO: Add tests for FromFirestore using a mocking framework (like Moq)
        //       or by creating mock DocumentSnapshot instances if possible.

        /*
        [Test] // NUnit attribute for test methods
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