using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyOrg.Client.Salesforce {
  public class SalesforceInsertException : Exception {
    public SalesforceInsertException() { }

    public SalesforceInsertException(string objectName, string jsonSerializedObject, string sfResponse)
      : base("An error occurred attempting to insert an object into salesforce." + Environment.NewLine +
             "Salesforce Response: " + sfResponse + Environment.NewLine +
             "Object: " + objectName + Environment.NewLine + jsonSerializedObject)
    { }
  }
  

  public class SalesforceHttpPostException : Exception {
    public SalesforceHttpPostException() { }

    public SalesforceHttpPostException(Exception e, string url, string objectName, string jsonSerializedObject)
      : base("An error occurred for an http post request." + Environment.NewLine +
             "Url:" + url + Environment.NewLine +
             "Exception: " + e.Message + Environment.NewLine +
             "Object: " + objectName + Environment.NewLine + jsonSerializedObject)
      { }
  }
  

  public class SalesforceHttpGetException : Exception {
    public SalesforceHttpGetException() { }

    public SalesforceHttpGetException(Exception e, string url)
      : base("An error occurred for an http get request." + Environment.NewLine +
             "Url:" + url + Environment.NewLine +
             "Exception: " + e.Message + Environment.NewLine) { }
  }
}