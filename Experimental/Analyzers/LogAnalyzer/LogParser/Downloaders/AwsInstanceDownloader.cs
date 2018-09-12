using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using ParserUtilities.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogParser.Downloaders {
    public class InstancesData {
        private List<Instance> Instances { get; set; }


        private DefaultDictionary<string, Instance> ByIP;

        public InstancesData(List<Instance> instances) {
            Instances = instances;
            ByIP = new DefaultDictionary<string, Instance>(x => Instances.FirstOrDefault(y => y.PrivateIpAddress.ToString() == x));
        }

        public Instance GetByIp(string sIp) {
            return ByIP[sIp];

        }
    }

    public class AwsInstanceDownloader {
        public static InstancesData GetAllInstances() {
            using (var client = new AmazonEC2Client(RegionEndpoint.USWest2)) {
                var response = client.DescribeInstances(new DescribeInstancesRequest {});
                var instances = response.Reservations.SelectMany(x => x.Instances).ToList();
                return new InstancesData(instances);
            }
        }
    }
}
