using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Handles Id responses when that's all that's needed.
namespace MyOrg.Models.Response {
  public class ResponseMemberId {
    public ResponseAttibute Attribute { get; set; }
    public string Id { get; set; }
  }
}