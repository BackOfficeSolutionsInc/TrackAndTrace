using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogParser.Models {
    public class AwsEnvironment {
        public AwsEnvironment(string name, string loadBalancerName, string region, string autoScalingGroup) {
            Name = name;
            LoadBalancerName = loadBalancerName;
            Region = region;
            AutoScalingGroup = autoScalingGroup;
        }

        public string Name { get; private set; }
        public string LoadBalancerName { get; private set; }
        public string Region { get; private set; }
        public string AutoScalingGroup { get; set; }

        public override string ToString() {
            return Name;
        }
    }
}
