using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sqlite
{
    public enum ChangeType : int
    {
        Add = 1,
        Remove = 3,
        Alter = 2,
        Warning = 4,
    }

    public enum ObjectType : int
    {
        Table=1,
        Column=2,
        DataType=3
    }
    public class Change
    {
        public Change()
        {
            Column = new Column();
        }

        public ChangeType ChangeType { get; set; }
        public ObjectType ObjectType { get; set; }
        public String Description { get; set; }
        public Column Column { get; set; }
        /*public string TableName { get; set; }
        public String Column { get; set; }
        public ColumnType? Type { get; set; }
        public bool? AutoIncrement { get; set; }
        public bool? Primary { get; set; }
        public bool? IsNull { get; set; }
        public string Default{ get; set; }*/

        public String CSV()
        {
            return String.Join(",", ChangeType, ObjectType, this.Column.TableName, this.Column.Name,this.Column.ColumnType, Description);
        }

        public static Change Create(ChangeType change, ObjectType oType, Column column, String desc)
        {
            return new Change()
            {
                ChangeType = change,
                ObjectType = oType,
                Column = column,
                Description = desc

            };
        }
        public static Change Create(ChangeType change, ObjectType oType, String table, String desc)
        {
            var output= new Change()
            {
                ChangeType = change,
                ObjectType = oType,
                Description = desc
            };
            output.Column.TableName = table;
            return output;
        }

    }
}
