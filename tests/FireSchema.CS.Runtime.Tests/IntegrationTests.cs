using NUnit.Framework;
using FireSchema.CS.Runtime.Core;
using Google.Cloud.Firestore;
using System;
using System.Threading.Tasks;
using System.Collections.Generic; // Required for Dictionary used in cleanup
using System.Linq; // For Enumerable methods like All

namespace FireSchema.CS.Runtime.Tests
{
    // Re-use the TestModel from FirestoreDataConverterTests for consistency
    // If it were in a separate file, we'd just use it directly.
    // For clarity here, let's assume it's accessible or redefine if needed.
    // [FirestoreData] public class TestModel : IFirestoreDocument { ... }

    // Define a concrete CollectionRef for testing purposes
    public class TestCollectionRef : BaseCollectionRef<TestModel>
    {
        public TestCollectionRef(FirestoreDb firestoreDb)
            : base(firestoreDb, "test_items") // Use a dedicated collection name for tests
        { }
    }

    [TestFixture]
    public class IntegrationTests
    {
        private FirestoreDb _firestoreDb = null!;
        private TestCollectionRef _testCollection = null!;
        private const string TestProjectId = "test-project-id"; // Use a dummy project ID for emulator

        private static readonly FirestoreDataConverter<TestModel> _converter = new FirestoreDataConverter<TestModel>(); // Add converter instance
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            // Point Firestore client to the emulator
            // Ensure the FIRESTORE_EMULATOR_HOST environment variable is set
            // (e.g., "localhost:8080" or "127.0.0.1:8080") before running tests.
            // Alternatively, configure FirestoreDbBuilder explicitly:
            Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", "localhost:8080"); // Set explicitly for clarity

            _firestoreDb = new FirestoreDbBuilder
            {
                ProjectId = TestProjectId,
                EmulatorDetection = Google.Api.Gax.EmulatorDetection.EmulatorOnly // Ensure we only connect to emulator
            }.Build();
        }

        [SetUp]
        public async Task Setup()
        {
            // Create a new collection reference for each test
            _testCollection = new TestCollectionRef(_firestoreDb);

            // Clear the collection before each test to ensure isolation
            await ClearCollectionAsync(_testCollection.FirestoreCollection);
        }

        [TearDown]
        public async Task TearDown()
        {
            // Optional: Clear the collection after each test as well
            await ClearCollectionAsync(_testCollection.FirestoreCollection);
        }

        // Helper method to delete all documents in a collection
        private async Task ClearCollectionAsync(CollectionReference collectionReference)
        {
            QuerySnapshot snapshot = await collectionReference.Limit(50).GetSnapshotAsync(); // Limit batch size
            while (snapshot.Count > 0)
            {
                var batch = _firestoreDb.StartBatch();
                foreach (var document in snapshot.Documents)
                {
                    batch.Delete(document.Reference);
                }
                await batch.CommitAsync();
                snapshot = await collectionReference.Limit(50).GetSnapshotAsync(); // Get next batch
            }
        }

        // --- Basic CRUD Tests ---

        [Test]
        public async Task AddAsync_ShouldCreateDocument_And_PopulateId()
        {
            // Arrange
            var newItem = new TestModel { Name = "Test Add", Value = 100 };

            // Act
            var addedItem = await _testCollection.AddAsync(newItem);

            // Assert
            Assert.That(addedItem, Is.Not.Null, "AddAsync should return a DocumentReference.");
            Assert.That(addedItem.Id, Is.Not.Null.And.Not.Empty, "Returned DocumentReference should have an ID.");

            // Verify the data was actually written by fetching it back
            var retrievedItem = await _testCollection.GetAsync(addedItem.Id);
            Assert.That(retrievedItem, Is.Not.Null, "GetAsync should retrieve the added item.");
            Assert.That(retrievedItem!.Id, Is.EqualTo(addedItem.Id), "ID should be populated by the converter");
            Assert.That(retrievedItem!.Name, Is.EqualTo(newItem.Name));
            Assert.That(retrievedItem.Value, Is.EqualTo(newItem.Value));

            // Verify directly from Firestore
            var snapshot = await _testCollection.Doc(addedItem.Id).GetSnapshotAsync();
            Assert.That(snapshot.Exists, Is.True);
            var data = snapshot.ConvertTo<TestModel>();
            Assert.That(data.Name, Is.EqualTo(newItem.Name));
        }

        [Test]
        public async Task GetAsync_ShouldRetrieveCorrectDocument()
        {
            // Arrange
            var docRef = _firestoreDb.Collection("test_items").Document(); // Get a new doc ref
            var originalItem = new TestModel { Id = docRef.Id, Name = "Test Get", Value = 200 };
            await docRef.SetAsync(originalItem); // Set directly for setup

            // Act
            var retrievedItem = await _testCollection.GetAsync(docRef.Id);

            // Assert
            Assert.That(retrievedItem, Is.Not.Null);
            Assert.That(retrievedItem!.Id, Is.EqualTo(originalItem.Id));
            Assert.That(retrievedItem!.Name, Is.EqualTo(originalItem.Name));
            Assert.That(retrievedItem.Value, Is.EqualTo(originalItem.Value));
        }

        [Test]
        public async Task SetAsync_ShouldOverwriteDocument()
        {
            // Arrange
            var docId = "set-test-id";
            var initialItem = new TestModel { Id = docId, Name = "Initial Set", Value = 300 };
            await _testCollection.Doc(docId).SetAsync(initialItem); // Use runtime SetAsync

            var updatedItem = new TestModel { Id = docId, Name = "Updated Set", Value = 301 };

            // Act
            await _testCollection.SetAsync(updatedItem.Id, updatedItem); // Overwrite using SetAsync(id, data)

            // Assert
            var retrievedItem = await _testCollection.GetAsync(docId);
            Assert.That(retrievedItem, Is.Not.Null);
            Assert.That(retrievedItem!.Name, Is.EqualTo(updatedItem.Name));
            Assert.That(retrievedItem.Value, Is.EqualTo(updatedItem.Value));
        }

         [Test]
        public async Task SetAsync_WithMerge_ShouldMergeDocument()
        {
            // Arrange
            var docId = "merge-test-id";
            var initialItem = new TestModel { Id = docId, Name = "Initial Merge", Value = 400 };
             // Use direct SetAsync for initial state to avoid depending on the method under test
            await _firestoreDb.Collection("test_items").Document(docId).SetAsync(new Dictionary<string, object>
            {
                { "name", initialItem.Name },
                { "value", initialItem.Value }
            });

            // Create an object with only the field to update
            var partialUpdate = new Dictionary<string, object> { { "value", 401 } };

            // Act
            // Use the SetAsync overload with SetOptions
            await _testCollection.SetAsync(docId, partialUpdate, SetOptions.MergeAll);

            // Assert
            var retrievedItem = await _testCollection.GetAsync(docId);
            Assert.That(retrievedItem, Is.Not.Null);
            Assert.That(retrievedItem!.Name, Is.EqualTo(initialItem.Name), "Name should be unchanged"); // Name should be unchanged
            Assert.That(retrievedItem.Value, Is.EqualTo(401)); // Value should be updated
        }


        [Test] // Uncommented now that UpdateAsync is implemented
        public async Task UpdateAsync_ShouldUpdateSpecificFields()
        {
            // Arrange
            var docId = "update-test-id";
            var initialItem = new TestModel { Id = docId, Name = "Initial Update", Value = 500 };
            await _testCollection.SetAsync(docId, initialItem); // Use SetAsync for setup

            // Act
            // Use the new UpdateAsync with the type-safe Set method in the builder
            await _testCollection.UpdateAsync(docId, builder => builder.Set(m => m.Value, 501));

            // Assert
            var retrievedItem = await _testCollection.GetAsync(docId);
            Assert.That(retrievedItem, Is.Not.Null);
            Assert.That(retrievedItem!.Name, Is.EqualTo(initialItem.Name), "Name should be unchanged");
            Assert.That(retrievedItem!.Value, Is.EqualTo(501), "Value should be updated");
        }

        [Test] // Uncommented now that DeleteAsync is implemented
        public async Task DeleteAsync_ShouldRemoveDocument()
        {
            // Arrange
            var docId = "delete-test-id";
            var itemToDelete = new TestModel { Id = docId, Name = "To Delete", Value = 600 };
            // Use SetAsync from BaseCollectionRef for setup consistency
            await _testCollection.SetAsync(docId, itemToDelete);

            // Act
            await _testCollection.DeleteAsync(docId); // Call the implemented method

            // Assert
            var snapshot = await _testCollection.Doc(docId).GetSnapshotAsync();
            Assert.That(snapshot.Exists, Is.False);
        }

        // --- Query Tests ---
        // TODO: Add Query tests (Where, OrderBy, Limit)

        [Test]
        public async Task Where_ShouldFilterResults()
        {
            // Arrange
            await _testCollection.AddAsync(new TestModel { Name = "Query Filter 1", Value = 700 });
            await _testCollection.AddAsync(new TestModel { Name = "Query Filter 2", Value = 701 });
            await _testCollection.AddAsync(new TestModel { Name = "Query Filter 3", Value = 701 });

            // Act
            var results = await _testCollection.Where(m => m.Value, QueryOperator.EqualTo, 701).GetAsync();

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results.All(m => m.Value == 701), Is.True);
        }

        [Test]
        public async Task OrderBy_ShouldSortResultsAscending()
        {
            // Arrange
            await _testCollection.AddAsync(new TestModel { Name = "Query Sort Z", Value = 802 });
            await _testCollection.AddAsync(new TestModel { Name = "Query Sort A", Value = 800 });
            await _testCollection.AddAsync(new TestModel { Name = "Query Sort M", Value = 801 });

            // Act
            var results = await _testCollection.OrderBy(m => m.Name).GetAsync();

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results[0].Name, Is.EqualTo("Query Sort A"));
            Assert.That(results[1].Name, Is.EqualTo("Query Sort M"));
            Assert.That(results[2].Name, Is.EqualTo("Query Sort Z"));
        }

        [Test]
        public async Task OrderByDescending_ShouldSortResultsDescending()
        {
            // Arrange
            await _testCollection.AddAsync(new TestModel { Name = "Query Sort Z", Value = 902 });
            await _testCollection.AddAsync(new TestModel { Name = "Query Sort A", Value = 900 });
            await _testCollection.AddAsync(new TestModel { Name = "Query Sort M", Value = 901 });

            // Act
            var results = await _testCollection.OrderByDescending(m => m.Value).GetAsync();

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results[0].Value, Is.EqualTo(902));
            Assert.That(results[1].Value, Is.EqualTo(901));
            Assert.That(results[2].Value, Is.EqualTo(900));
        }

        [Test]
        public async Task Limit_ShouldLimitResults()
        {
            // Arrange
            await _testCollection.AddAsync(new TestModel { Name = "Query Limit 1", Value = 1000 });
            await _testCollection.AddAsync(new TestModel { Name = "Query Limit 2", Value = 1001 });
            await _testCollection.AddAsync(new TestModel { Name = "Query Limit 3", Value = 1002 });

            // Act
            var results = await _testCollection.OrderBy(m => m.Value).Limit(2).GetAsync();

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results[0].Value, Is.EqualTo(1000));
            Assert.That(results[1].Value, Is.EqualTo(1001));
        }


        [Test]
        public async Task WhereIn_ShouldFilterResults()
        {
            // Arrange
            await _testCollection.AddAsync(new TestModel { Name = "In 1", Value = 1100 });
            await _testCollection.AddAsync(new TestModel { Name = "In 2", Value = 1101 });
            await _testCollection.AddAsync(new TestModel { Name = "In 3", Value = 1102 });
            await _testCollection.AddAsync(new TestModel { Name = "In 4", Value = 1103 });
            var valuesToInclude = new List<int> { 1101, 1103 };

            // Act
            var results = await _testCollection.WhereIn(m => m.Value, valuesToInclude).GetAsync();

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results.All(m => valuesToInclude.Contains(m.Value)), Is.True);
            Assert.That(results.Any(m => m.Name == "In 2"), Is.True);
            Assert.That(results.Any(m => m.Name == "In 4"), Is.True);
        }

        [Test]
        public async Task WhereNotIn_ShouldFilterResults()
        {
            // Arrange
            await _testCollection.AddAsync(new TestModel { Name = "NotIn 1", Value = 1200 });
            await _testCollection.AddAsync(new TestModel { Name = "NotIn 2", Value = 1201 });
            await _testCollection.AddAsync(new TestModel { Name = "NotIn 3", Value = 1202 });
            await _testCollection.AddAsync(new TestModel { Name = "NotIn 4", Value = 1203 });
            var valuesToExclude = new List<int> { 1201, 1203 };

            // Act
            var results = await _testCollection.WhereNotIn(m => m.Value, valuesToExclude).GetAsync();

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results.All(m => !valuesToExclude.Contains(m.Value)), Is.True);
            Assert.That(results.Any(m => m.Name == "NotIn 1"), Is.True);
            Assert.That(results.Any(m => m.Name == "NotIn 3"), Is.True);
        }

        [Test]
        public async Task WhereArrayContainsAny_ShouldFilterResults()
        {
            // Arrange
            await _testCollection.AddAsync(new TestModel { Name = "ACA 1", Tags = new List<string> { "a", "b" } });
            await _testCollection.AddAsync(new TestModel { Name = "ACA 2", Tags = new List<string> { "c", "d" } });
            await _testCollection.AddAsync(new TestModel { Name = "ACA 3", Tags = new List<string> { "b", "e" } });
            await _testCollection.AddAsync(new TestModel { Name = "ACA 4", Tags = new List<string> { "f" } });
            var tagsToInclude = new List<string> { "b", "d" };

            // Act
            var results = await _testCollection.WhereArrayContainsAny(m => m.Tags, tagsToInclude).GetAsync();

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count, Is.EqualTo(3)); // Should match ACA 1, ACA 2, ACA 3
            Assert.That(results.Any(m => m.Name == "ACA 1"), Is.True);
            Assert.That(results.Any(m => m.Name == "ACA 2"), Is.True);
            Assert.That(results.Any(m => m.Name == "ACA 3"), Is.True);
            Assert.That(results.Any(m => m.Name == "ACA 4"), Is.False);
        }

        [Test]
        public async Task Pagination_StartAfter_ShouldGetNextPage()
        {
            // Arrange: Add 5 items ordered by Value
            for (int i = 1; i <= 5; i++)
            {
                await _testCollection.AddAsync(new TestModel { Name = $"Page {i}", Value = 1300 + i });
            }

            // Act 1: Get the first page (2 items)
            var firstPageSnapshot = await _testCollection.OrderBy(m => m.Value).Limit(2).GetSnapshotAsync();
            // Use the public converter instance now
            var firstPageDocs = firstPageSnapshot.Documents.Select(doc => _converter.FromFirestore(doc)).Where(item => item != null).ToList(); // Filter out potential nulls

            // Assert 1: Check first page content
            // Ensure correct Assert.That syntax
            Assert.That(firstPageDocs.Count, Is.EqualTo(2)); // This syntax should be correct
            Assert.That(firstPageDocs[0]!.Value, Is.EqualTo(1301));
            Assert.That(firstPageDocs[1]!.Value, Is.EqualTo(1302));

            // Act 2: Get the snapshot of the last document from the first page
            var lastDocSnapshot = firstPageSnapshot.Documents.LastOrDefault();
            Assert.That(lastDocSnapshot, Is.Not.Null, "Should have a last document in the first page");

            // Act 3: Get the second page (next 2 items) using StartAfter
            var secondPageDocs = await _testCollection.OrderBy(m => m.Value)
                                                    .StartAfter(lastDocSnapshot!)
                                                    .Limit(2)
                                                    .GetAsync();

            // Assert 3: Check second page content
            Assert.That(secondPageDocs.Count, Is.EqualTo(2));
            Assert.That(secondPageDocs[0].Value, Is.EqualTo(1303));
            Assert.That(secondPageDocs[1].Value, Is.EqualTo(1304));

             // Act 4: Get the third page (last item)
            var lastDocSnapshotPage2 = await _testCollection.OrderBy(m => m.Value)
                                                    .StartAfter(lastDocSnapshot!)
                                                    .Limit(2)
                                                    .GetSnapshotAsync(); // Need snapshot again
            var lastDocSnapshotP2 = lastDocSnapshotPage2.Documents.LastOrDefault();
            Assert.That(lastDocSnapshotP2, Is.Not.Null);

            var thirdPageDocs = await _testCollection.OrderBy(m => m.Value)
                                                   .StartAfter(lastDocSnapshotP2!)
                                                   .Limit(2)
                                                   .GetAsync();

            // Assert 4: Check third page content
            Assert.That(thirdPageDocs.Count, Is.EqualTo(1));
            Assert.That(thirdPageDocs[0].Value, Is.EqualTo(1305));
        }


        [Test]
        public async Task UpdateAsync_WithIncrement_ShouldIncrementValue()
        {
            // Arrange
            var docId = "increment-test-id";
            var initialItem = new TestModel { Id = docId, Name = "Increment Test", Value = 50 };
            await _testCollection.SetAsync(docId, initialItem);

            // Act
            await _testCollection.UpdateAsync(docId, builder => builder.Increment(m => m.Value, 10));

            // Assert
            var retrievedItem = await _testCollection.GetAsync(docId);
            Assert.That(retrievedItem, Is.Not.Null);
            Assert.That(retrievedItem!.Value, Is.EqualTo(60), "Value should be incremented by 10.");
            Assert.That(retrievedItem!.Name, Is.EqualTo(initialItem.Name), "Name should remain unchanged.");
        }

        // --- Update Builder Tests ---

        [Test]
        public async Task UpdateAsync_WithArrayUnion_ShouldAddUniqueElements()
        {
            // Arrange
            var docId = "array-union-test-id";
            var initialItem = new TestModel 
            {
                 Id = docId, 
                 Name = "Array Union Test", 
                 Value = 1100,
                 Tags = new List<string> { "a", "b" } 
            };
            await _testCollection.SetAsync(docId, initialItem);

            // Act: Add "c" (new) and "a" (existing)
            await _testCollection.UpdateAsync(docId, builder => builder.ArrayUnion(m => m.Tags, "c", "a"));

            // Assert
            var retrievedItem = await _testCollection.GetAsync(docId);
            Assert.That(retrievedItem, Is.Not.Null);
            Assert.That(retrievedItem!.Tags, Is.EquivalentTo(new List<string> { "a", "b", "c" }), "Should contain a, b, c.");
        }

        [Test]
        public async Task UpdateAsync_WithArrayRemove_ShouldRemoveElements()
        {
            // Arrange
            var docId = "array-remove-test-id";
            var initialItem = new TestModel 
            {
                 Id = docId, 
                 Name = "Array Remove Test", 
                 Value = 1200,
                 Tags = new List<string> { "x", "y", "z", "y" } // Duplicate 'y'
            };
            await _testCollection.SetAsync(docId, initialItem);

            // Act: Remove "y" (both instances) and "a" (non-existent)
            await _testCollection.UpdateAsync(docId, builder => builder.ArrayRemove(m => m.Tags, "y", "a"));

            // Assert
            var retrievedItem = await _testCollection.GetAsync(docId);
            Assert.That(retrievedItem, Is.Not.Null);
            Assert.That(retrievedItem!.Tags, Is.EquivalentTo(new List<string> { "x", "z" }), "Should contain only x, z.");
        }

        // TODO: Add more complex Update tests

    } // End of IntegrationTests class
} // End of namespace