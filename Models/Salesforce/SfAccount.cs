using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MyOrg.Models.Salesforce {
  public class SfAccount {
    // Don't serialize the id when posting updates since Salesforce API will throw an error
    [JsonIgnore]
    public string Id { get; set; }
    public string Name { get; set; }
  }
}