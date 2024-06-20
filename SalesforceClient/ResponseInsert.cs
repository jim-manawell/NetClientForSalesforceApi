using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyOrg.Client.Salesforce {
  public class ResponseInsert {
    public string Id { get; set; }
    public bool Success { get; set; }
    public List<string> Errors { get; set; }
  }
}
