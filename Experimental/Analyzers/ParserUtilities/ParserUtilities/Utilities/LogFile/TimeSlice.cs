using ParserUtilities.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserUtilities.Utilities.LogFile {
    public class TimeSlice {

        public TimeSlice(DateTime time,DateTimeKind kind, string name) 
            : this(TimeRange.Around(0,time,kind),name){}


        public TimeSlice(TimeRange time, string name) {
            Range = time;
            Name = name;
        }

        public TimeRange Range { get; set; }
        public String Name { get; set; }
    }
}
