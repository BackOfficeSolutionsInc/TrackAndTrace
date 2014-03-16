using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConvertMysqlToSqlite
{
    public enum DefinitionType : int
    {
        Header = 0,
        Table = 1,
        Data = 2,
        Ending=3,
    }
    /*
    public abstract class DynamicWrapper<T> : T
    {
        public dynamic AdditionalProperties { get; set; }

        public static DynamicWrapper<T> Create<T>(T item)
	    {
            return item as DynamicWrapper<T>;
	    }
    }*/

    public class Definition
    {
        public DefinitionType DefinitionType {get;set;}

        public String TableName { get; set; }
        public List<String> Lines { get; set; }
        public List<String> References { get; set; }        
        public HashSet<Definition> ReferenceNodes { get; set; }

        public Definition(List<string> lines,DefinitionType type,String tableName,List<string> references)
	    {
            Lines = lines;
            DefinitionType = type;
            TableName = tableName;
            References = references;
            ReferenceNodes = new HashSet<Definition>();
	    }

        public override bool Equals(object obj)
        {
            if (obj is Definition)
                return ((Definition)obj).TableName == this.TableName;
            return false;
        }

        public override string ToString()
        {
            return DefinitionType+"- "+TableName;
        }

        public override int GetHashCode()
        {
            return TableName.GetHashCode();
        }
    }

    public class ExtractDefinitions
    {
        public static List<Definition> OrderDefinitions(List<Definition> defs)
        {
            //defs.Select(x => DynamicWrapper<Definition>.Create(x)).ToList();
/*
            foreach (var d in defs)
            {
                foreach (var r in d.References)
                {
                    d.ReferenceNodes.Add(defs.FirstOrDefault(x=>x.TableName==r));
                }
            }

            var ordered=new List<Definition>();
            var currentlyContains = new List<String>();

            var allDefs=defs.Select(x=>x).Where(x=>x.DefinitionType==DefinitionType.Table).ToList();
            ordered.Add(defs.FirstOrDefault(x => x.DefinitionType == DefinitionType.Header));

            while(allDefs.Any())
            {
                Definition dd=null;
                bool all = false;
                foreach (var d in allDefs)
                {
                    dd = d;
                    all = false;
                    if (d.References.All(x => currentlyContains.Contains(x)))
                    {
                        ordered.Add(d);
                        ordered.Add(defs.FirstOrDefault(x => x.DefinitionType == DefinitionType.Data && x.TableName==d.TableName));
                        currentlyContains.Add(d.TableName);
                        break;
                    }
                    all = true;
                }
                if (dd != null && all==false)
                {
                    allDefs.Remove(dd);
                }
                else
                {
                    int a = 0;
                }
            }*/

            return defs.OrderBy(x=>(int)x.DefinitionType).ToList();
        }


        public static List<Definition> ExtractAll(List<String> lines)
        {
            var output=new List<Definition>();
            var references = new List<String>();
            var type = DefinitionType.Header;
            var tableName = "<HEADER>";

            var tempLines = new List<String>();
            
            foreach (var line in lines)
            {
                if (line.Contains("REFERENCES"))
                {
                    var refInd=line.IndexOf("REFERENCES")+"REFERENCES".Length+2;
                    var reff=line.Substring(refInd,line.IndexOf("\"",refInd)-refInd);
                    references.Add(reff);
                }

                tempLines.Add(line);
                if (line.Contains("Table structure for table"))
                {
                    output.Add(ExtractDefinition(tempLines, type, 3,tableName,references));
                    tableName=line.Split('"')[1];
                    type = DefinitionType.Table;          
                }                
                if (line.Contains("Dumping data for table"))
                {
                    output.Add(ExtractDefinition(tempLines, type, 3, tableName,references));
                    tableName=line.Split('"')[1];
                    type = DefinitionType.Data;          
                }
            }

            output.Add(ExtractDefinition(tempLines, type, 10, tableName, references));
            type = DefinitionType.Ending;
            output.Add(ExtractDefinition(tempLines, type, 0, "<ENDING>", references));
            


            return OrderDefinitions(output);
        }

        public static Definition ExtractDefinition(List<String> tempList,DefinitionType type, int n,String tableName,List<string> references)
        {
            var newList = new List<String>();
            var count =  tempList.Count - n;
            for (int i = 0; i < count; i++)
            {
                newList.Add(tempList[0]);
                tempList.RemoveAt(0);
            }
            if (type == DefinitionType.Table)
            {
                newList = newList.Select(x => x.Replace("NOT NULL   '", "NOT NULL DEFAULT '")).ToList();
            }

            var output=new Definition(newList, type,tableName,references.Select(x=>x).ToList());
            references.Clear();

            return output;

        }

        public static List<string> RemoveLastN(List<String> oldList, int n)
        {
            var newList = new List<String>();
            for (int i = 0; i < n; i++)
            {
                newList.Add(oldList[oldList.Count - 1]);
                oldList.RemoveAt(oldList.Count - 1);
            }
            return newList;
        }

    }
}
