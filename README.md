# FireSchema.CS.Runtime 🔥📄

[![NuGet version](https://badge.fury.io/nu/FireSchema.CS.Runtime.svg)](https://badge.fury.io/nu/FireSchema.CS.Runtime)

C# Runtime Library for code generated by FireSchema. Provides base classes and helpers for interacting with Google Cloud Firestore using the `Google.Cloud.Firestore` SDK.

This library is intended to be used with code generated by the `@shtse8/fireschema` tool.

## Installation

1.  **Install the NuGet Package:**
    ```bash
    dotnet add package FireSchema.CS.Runtime
    # Or via Package Manager Console:
    # Install-Package FireSchema.CS.Runtime 
    ```
2.  **Install the Firestore SDK:** Ensure you also have the official Google Cloud Firestore SDK installed:
    ```bash
    dotnet add package Google.Cloud.Firestore
    ```

## Basic Usage Example

*(Assumes you have defined a `users` collection in `firestore.schema.json`, configured the `csharp-client` target in `fireschema.config.json`, and run `fireschema generate`)*

```csharp
using Google.Cloud.Firestore;
using YourProject.Generated.Firestore; // Adjust namespace
using YourProject.Generated.Firestore.Users; // Adjust namespace

// Your initialized FirestoreDb instance
FirestoreDb firestoreDb = FirestoreDb.Create("your-project-id"); 

var usersCollection = new UsersCollection(firestoreDb);
var userId = "user-csharp-123";

async Task CSharpExample()
{
    // Add
    var newUser = new UsersAddData { DisplayName = "CSharp User", Email = "csharp@example.com", Age = 30 };
    var newUserRef = await usersCollection.AddAsync(newUser);
    Console.WriteLine($"Added: {newUserRef.Id}");

    // Get
    var fetchedUser = await usersCollection.GetAsync(newUserRef.Id);
    Console.WriteLine($"Fetched: {fetchedUser?.Data.DisplayName}");

    // Query
    var activeUsers = await usersCollection.Query()
        .WhereIsActive(FilterOperator.EqualTo, true) // Assumes IsActive field
        .Limit(10)
        .GetDataAsync();
    Console.WriteLine($"Active Users: {activeUsers.Count}");

    // Update
    await usersCollection.UpdateAsync(newUserRef.Id)
        .IncrementAge(1) // Assumes Age field
        .SetLastLogin(Timestamp.GetCurrentTimestamp()) // Assumes LastLogin field
        .CommitAsync();
    Console.WriteLine($"Updated: {newUserRef.Id}");
}

// Remember to call the example function
// await CSharpExample(); 
```

**➡️ See the full [C# Client Guide](https://shtse8.github.io/FireSchema/guide/csharp-client.html) (Coming Soon!) in the main documentation.**