using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ContextAwareSearchWebAPI.Models
{
    public class SearchResult
    {
        public string title { get; set; }
        public string description { get; set; }
        public string url { get; set; }
        public int ranking { get; set; }
    }
}