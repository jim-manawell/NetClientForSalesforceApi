using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// These fields are included in Salesforce responses
namespace MyOrg.Models.Response {
  public class ResponseAttibute {
    public string Type { get; set; }
    public string Url { get; set; }
  }
}