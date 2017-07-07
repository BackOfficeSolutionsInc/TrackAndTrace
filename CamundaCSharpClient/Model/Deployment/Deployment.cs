using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CamundaCSharpClient.Model.Deployment
{
    public class Deployment
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Source { get; set; }

        public override string ToString() => $"Deployment [Id={Id}, Name={Name}]"; 
    }
}
