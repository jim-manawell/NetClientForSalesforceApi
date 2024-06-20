using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyOrg.Client.Salesforce {
  public class ResponseQuery<T> {
    public string TotalSize { get; set; }
    public bool Done { get; set; }
    public string NextRecordsUrl { get; set; }
    public List<T> Records { get; set; } // T is the saleforce object type
  }
}