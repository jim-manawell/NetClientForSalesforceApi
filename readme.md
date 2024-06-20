# .NET Client for Salesforce API

### What is it for?
This C# library simplifies interacting with the Salesforce REST API, allowing you to query and update data in a more user-friendly way. It offers a comprehensive interface that mirrors the functionality of the Salesforce REST API.


### Features
* **Simplified Object Interaction:** Interact with Salesforce objects using strongly-typed classes.
* **CRUD Operations:** Perform Create, Read, Update, and Delete (CRUD) operations on Salesforce data.
* **SOQL Queries:** Execute SOQL (Salesforce Object Query Language) and SOSL (Salesforce Object Search Language) queries for efficient data retrieval.
* **Bulk API Support:** Leverage the power of the Salesforce Bulk API for large data operations.
Authentication: Manages OAuth 2.0 authentication for secure connections to your Salesforce instance.

### Getting Started
1. **Prerequisites:** .NET Framework 3.5.1 or later
2. Clone this repository or download the source code.
3. Add references to the cs files 
4. Update the App.config file or better yet use Key Vault to store Salesforce secrets

```cs
using MyOrg.Client.Salesforce;

// Salesforce configuration
var salesforceClientId = client.GetASecretValue("salesforceClientID");
var salesforceClientSecret = client.GetASecretValue("salesforceClientSecret");
var salesforceUsername = client.GetASecretValue("salesforceUsername");
var salesforcePassword = client.GetASecretValue("salesforcePassword");
var salesforceToken = client.GetASecretValue("salesforceToken");
var salesforceHost = client.GetASecretValue("salesforceHost");
var salesforceUserProfileId = client.GetASecretValue("salesforceUserProfileId");
var salesforceLoginEndpoint = client.GetASecretValue("salesforceLoginEndpoint");
var salesforceApiEndpoint = client.GetASecretValue("salesforceApiEndpoint");
var salesforceBulkEndpoint = client.GetASecretValue("salesforceBulkEndpoint");

var salesforceClient = new SalesforceClient(salesforceClientId, salesforceClientSecret, salesforceUsername, salesforcePassword, salesforceToken, salesforceHost, salesforceLoginEndpoint, salesforceApiEndpoint, salesforceBulkEndpoint);
```

### Usage
Get Contact
```cs
public SfContact GetContact(String contactId) {
  if (string.IsNullOrEmpty(contactId)) throw new ArgumentNullException("Parameter cannot be null or empty", "contactId");

  // Build query
  var query = string.Format("SELECT Id, Name FROM Contact WHERE Id = '{0}'", contactId);
  var response = _salesforceClient.Query(query);
  var contactsFound = JsonConvert.DeserializeObject<ResponseQuery<ResponseContact>>(response);

  if (contactsFound.Records.Count > 0) {
      var contact = itemsFound.Records.FirstOrDefault();
      return contact.Id
  }
  return null;
}
```

Mutate Contact
```cs
public void CreateContact(Contact sfContact) {
    if (sfContact == null) throw new ArgumentNullException("Parameter cannot be null", "sfContact");
    try
    {
        sfContact.SerializeJson = true;
        sfContact.Id = _salesforceClient.InsertObject<SfContact>("Contact", sfContact);
    }
    catch (SalesforceInsertException ex)
    {
        // handle/log exception here.
    }
}

// Mutate/Insert Contact
public void UpdateContact(Contact sfContact) {
    if (sfContact == null) throw new ArgumentNullException("Parameter cannot be null", "sfContact");
    try
    {
        sfContact.SerializeJson = true;
        sfContact.Id = _salesforceClient.UpdateObject<SfContact>("Contact", sfContact);
    }
    catch (SalesforceInsertException ex)
    {
        // handle/log exception here.
    }
}
```

Buk API Query
```cs
public List<SfContact> GetContacts(String name)
{
   if (contact == null) throw new ArgumentNullException("Parameter cannot be null", "contact");

    var query = @"SELECT Id, Name FROM Contact WHERE Name = '" + name + "'";
    string response = _salesforceClient.BulkApiQuery("Contact", query);
    return JsonConvert.DeserializeObject<List<SfContact>>(response);
}
```

### License
This project is licensed under the MIT License. See the LICENSE file for more information.

### Disclaimer
This is an open-source project and is not affiliated with Salesforce.com.