using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MyOrg.Models.Salesforce {
  public class SfContact {
    // Don't serialize the id when posting updates since Salesforce API will throw an error
    [JsonIgnore]
    public string Id { get; set; } 
    public string AccountId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Title { get; set; }
    public DateTime? MyCustomField { get; set; }

    // Tells the json serializer whether to ignore properties for update/insert operations
    // Set this flag to True for Insert and Query operations
    // Set this flag to False for Update operations
    [JsonIgnore]
    public bool SerializeJson { get; set; } = true;
    public bool ShouldSerializeMyCustomField() {
      return SerializeJson;
    }
  }
}