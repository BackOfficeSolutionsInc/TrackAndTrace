using Amazon.CloudWatch.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogParser.Models {
    public class Stat {
        public Stat(string name, string metric, string nameSpace, string statistic, double? min = null, double? max = null) {
            Name = name;
            Metric = metric;
            Namespace = nameSpace;
            Statistic = statistic;
            Min = min;
            Max = max;
            //Unit = unit;
        }
        public string Name { get; set; }
        public string Metric { get; set; }
        public string Namespace { get; set; }
        public string Statistic { get; set; }
        public double? Min { get; set; }
        public double? Max { get; set; }

        public Dimension GetDimension(AwsEnvironment env) {
            switch (Namespace) {
                case "AWS/ELB": return new Dimension() { Name = "LoadBalancerName", Value = env.LoadBalancerName };
                case "AWS/EC2": return new Dimension() { Name = "AutoScalingGroupName", Value = env.AutoScalingGroup };
                case "AWS/RDS": return new Dimension() { Name = "DBInstanceIdentifier", Value = "radial-enc" };
            }
            throw new Exception("Namespace unhandled:" + Namespace);
        }

        public double? GetValue(Datapoint d) {
            switch (Statistic) {
                case "Average":
                    return d.Average;
                case "Sum":
                    return d.Sum;
                case "Maximum":
                    return d.Maximum;
                case "Minimum":
                    return d.Minimum;
                case "SampleCount":
                    return d.SampleCount;
            }
            return null;
        }

        public Point ToPoint(Datapoint d) {
            return new Point(d.Timestamp, (decimal?)GetValue(d));
        }
    }
}
