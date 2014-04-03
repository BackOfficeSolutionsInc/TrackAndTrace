using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sqlite
{
    public enum ColumnType
    {
        bigint,
        tinyint,
        datetime,
        @decimal,
        @int,
        text,
        varchar,
        intQ,
        @double,
    }

    public class Column
    {
        public String DbType {get;set;}
        public String Name { get; set; }
        public String Default { get; set; }
        public bool IsNull { get; set; }
        public ColumnType ColumnType { get; set; }
        public String TableName { get; set; }
        public bool AutoIncrement { get; set; }
        public bool Primary { get; set; }

        public List<String> Differences(Column o)
        {
            var output=new List<String>();
            if (Name != o.Name)
                output.Add("Name");
            /*if(Default == o.Default)
                output.Add("Default");            
            if (IsNull == o.IsNull)
                output.Add("IsNull");*/
            if (ColumnType != ColumnType.intQ && o.ColumnType != ColumnType.intQ && ColumnType != o.ColumnType)
                output.Add("Type");
            if (TableName != o.TableName)
                output.Add("TableName");
            return output;  

        }

        public static Column FromChange(Change change)
        {
            if (change.ObjectType != ObjectType.Column)
                throw new Exception("not a column");
            return change.Column;
        }


        public String Csv()
        {
            return String.Format("{0},{1},{2},{3},{4}", DbType, Name, ColumnType, TableName, AutoIncrement, Primary);
           // return String.Format("{0},{1},{2},{3},{4},{5}", DbType, Name, Default, IsNull, Type, TableName);
        }
    }
}
