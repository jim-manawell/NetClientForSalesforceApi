using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyOrg.Models.Salesforce;

// Wraps Salesforce objects to handle Salesforce API responses
namespace MyOrg.Models.Response {
  public class ResponseAccount : SfAccount {
    public ResponseAttibute Attribute { get; set; }
  }
}