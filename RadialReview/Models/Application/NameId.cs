using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Application
{
    [DebuggerDisplay("{Id},{Name}")]
    public class NameId
    {
        public long Id { get;set;}
        public string Name { get; set; }

        public NameId(string name,long id)
        {
            Name = name;
            Id= id;
        }
    }
}