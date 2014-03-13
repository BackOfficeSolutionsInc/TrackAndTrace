using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sqlite
{
    public class CompaireDbs
    {
        public class Compare<T, TProp> : IEqualityComparer<T>
        {
            public Func<T, TProp> Func { get; set; }
            public Compare(Func<T, TProp> propFunc)
            {
                Func = propFunc;
            }

            public bool Equals(T x, T y)
            {
                return Func(x).Equals(Func(y));
            }

            public int GetHashCode(T obj)
            {
                return Func(obj).GetHashCode();
            }
        }

        public static List<String> SuggestUpdateCommands(IConnection past,List<Change> changes)
        {
            var commands = new List<String>();
            var toChange = changes.Select(x => x).ToList();

            foreach (var addTable in toChange.Where(x => x.ChangeType == ChangeType.Add && x.ObjectType == ObjectType.Table).ToList())
            {
                toChange.Remove(addTable);
                var columnsChange=toChange.Where(x => x.ChangeType == ChangeType.Add && x.ObjectType == ObjectType.Column && x.Column.TableName==addTable.Column.TableName).ToList();

                foreach(var toRemove in columnsChange){
                    toChange.Remove(toRemove);
                }

                var columns = columnsChange.Select(x => Column.FromChange(x));
                commands.Add(past.AddTableSyntax(addTable.Column.TableName, columns.ToArray()));                
            }
            
            var allAddColumn= toChange.Where(x => x.ChangeType == ChangeType.Add && x.ObjectType == ObjectType.Column).ToList();
            foreach (var addColumn in allAddColumn)
            {
                toChange.Remove(addColumn);
                commands.Add(past.AddColumnSyntax(addColumn.Column.TableName, Column.FromChange(addColumn)));
            }

            foreach (var alterColumn in toChange.Where(x => x.ChangeType == ChangeType.Alter && x.ObjectType == ObjectType.Column).ToList())
            {
                toChange.Remove(alterColumn);
                commands.Add(past.AlterColumnSyntax(alterColumn.Column.TableName, Column.FromChange(alterColumn)));
            }

            return commands;
            
        }

        public static List<Change> Compair(IConnection past, IConnection present, bool includeIsNull = true,bool includeDegrasion = true)
        {
            var pastColumns = DbAccess.GetColumns(past);
            var presentColumns = DbAccess.GetColumns(present);

            var pastTables = pastColumns.Select(x => x.TableName).Distinct();
            var presentTables = presentColumns.Select(x => x.TableName).Distinct();

            var changes = new List<Change>();

            foreach (var removed in pastTables.Except(presentTables))
            {
                changes.Add(Change.Create(ChangeType.Remove,ObjectType.Table,removed, "Removed table " + removed));
            }

            foreach (var added in presentTables.Except(pastTables))
            {
                changes.Add(Change.Create(ChangeType.Add,ObjectType.Table,added, "Added table " + added));
            }

            var alreadyAlter = new HashSet<Tuple<String, String>>();
            var allAdded=presentColumns.Except(pastColumns, new Compare<Column, Tuple<String, String, ColumnType,bool,bool,bool,string>>(x => 
                Tuple.Create(x.TableName, x.Name, x.ColumnType,x.AutoIncrement,x.IsNull || !includeIsNull,x.Primary,x.Default)));
            foreach (var added in allAdded)
            {
                var altered = pastColumns.FirstOrDefault(x => x.TableName == added.TableName && x.Name == added.Name);
                if (altered!=null)
                {
                    if (added.ColumnType==ColumnType.intQ || altered.ColumnType==ColumnType.intQ){
                        changes.Add(Change.Create(ChangeType.Warning,ObjectType.DataType,added,"Warning: compare types for " + added.Name + " type "+altered.ColumnType+" <==> " + added.ColumnType));
                    }else{
                        var changeType = ChangeType.Warning;
                        var description = "";
                        var cs = new List<String>();
                        if (altered.Primary != added.Primary){
                            changeType = ChangeType.Alter;
                            cs.Add("primary key from " + altered.Primary + " to " + added.Primary);
                        }
                        if (altered.IsNull != added.IsNull){
                            if (altered.IsNull && !added.IsNull)
                                changeType = ChangeType.Alter;
                            cs.Add("Is Nullable from " + altered.IsNull + " to " + added.IsNull);
                        }
                        if (altered.AutoIncrement != added.AutoIncrement)
                        {
                            changeType = ChangeType.Alter;
                            cs.Add("AutoIncrement from " + altered.AutoIncrement + " to " + added.AutoIncrement);
                        }
                        if (altered.Default != added.Default)
                        {
                            if (!string.IsNullOrEmpty(added.Default))
                                changeType = ChangeType.Alter;
                            cs.Add("Default from '" + altered.Default + "' to '" + added.Default + "'");
                        }
                        if (altered.ColumnType != added.ColumnType)
                        {
                            var isTextType = added.ColumnType == ColumnType.text && altered.ColumnType == ColumnType.varchar || altered.ColumnType == ColumnType.text && added.ColumnType == ColumnType.varchar;

                            if (!isTextType)
                                changeType = ChangeType.Alter;
                            cs.Add("ColumnType from " + altered.ColumnType + " to " + added.ColumnType);
                        }

                        description += String.Join(", ", cs);

                        changes.Add(Change.Create(changeType, ObjectType.DataType, added, description));
                    }
                    alreadyAlter.Add(Tuple.Create(added.TableName,added.Name));
                }
                else
                {
                    changes.Add(Change.Create(ChangeType.Add, ObjectType.Column, added, "Added column " + added.Name));
                }
            }
            foreach (var removed in pastColumns.Except(presentColumns, new Compare<Column, Tuple<String, String, ColumnType>>(x => Tuple.Create(x.TableName, x.Name, x.ColumnType))))
            {
                if (!alreadyAlter.Contains(Tuple.Create(removed.TableName, removed.Name)))
                {
                    changes.Add(Change.Create(ChangeType.Remove, ObjectType.Column, removed, "Removed column " + removed.Name));
                }
            }            

            return changes;
            /*

            var mysqlMismatchs = mysqlColumns.Where(mCol =>{
                return !sqliteColumns.Any(sCol => mCol.Differences(sCol).Count == 0);
            }).ToList();
            var sqliteMismatchs = sqliteColumns.Where(sCol => !mysqlColumns.Any(mCol => mCol.Differences(sCol).Count == 0)).ToList();

            return mysqlColumns.Union(sqliteColumns).ToList();*/
        }




    }
}
