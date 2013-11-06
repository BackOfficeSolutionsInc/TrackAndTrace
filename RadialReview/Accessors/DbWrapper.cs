using RadialReview.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web;

namespace RadialReview.Accessors
{/*
    public class DbWrapper<T> : IDisposable where T : ApplicationDbContext, new()
    {
        public class Entity
        {
            public object[] PrimaryKey { get; set; }
            public object Contents { get; set; }
            public Type Type { get; set; }
            public object DbContents {get;set;}

            protected static Boolean CompareKeys(Entity one, Entity two)
            {
                for (int i = 0; i < one.PrimaryKey.Length; i++)
                {
                    dynamic a = one.PrimaryKey[i];
                    dynamic b = two.PrimaryKey[i];
                    if (a.Equals(b) == false)
                        return false;
                }
                return true;
            }

            public override bool Equals(object obj)
            {
                if (obj is Entity)
                {
                    var entity = (Entity)obj;
                    if (entity.Type == this.Type && entity.PrimaryKey.Length == this.PrimaryKey.Length)
                        return CompareKeys(this, entity);
                }
                return false;
            }
            public override int GetHashCode()
            {
                var hash = 17;
                foreach (var key in PrimaryKey)
                {
                    hash = hash * 31 + key.GetHashCode();
                }
                return hash;
            }
        }

        public T Context;
        protected long Changes = 0;
        private int PreviousUnique = -1;
        protected List<Entity> Entities = null;
        protected List<Entity> ChangedEntities = new List<Entity>();
        protected List<Entity> AttachedEntities = new List<Entity>();
        protected Dictionary<Type, List<object>> Added = new Dictionary<Type, List<object>>();
        protected static Dictionary<Type, PropertyInfo[]> KeyDictionary = new Dictionary<Type, PropertyInfo[]>();

        public DbWrapper()
        {
            Context = new T();
        }

        public void Update<U>(U entity)
        {
            Entities = new List<Entity>();
            var type = typeof(U);
            Alter(entity, type);
            Entities = null;
            Changes++;
        }

        public int Save()
        {
            InternalUpdate();
            var output = Context.SaveChanges();
            Added.ToList().ForEach(a=>a.Value.ToList().ForEach(e=>AttachedEntities.Add(new Entity { Contents = e, PrimaryKey = GetKeysValues(e, a.Key), Type = a.Key })));
            ChangedEntities = new List<Entity>();           
            Added = new Dictionary<Type,List<object>>();
            return output;
        }

        protected void InternalUpdate()
        {
            foreach (var change in ChangedEntities)
            {
                var set = Context.Set(change.Type);
                var items = AttachedEntities.Select(x => new { name = x.Type.Name, primaryKey = x.PrimaryKey, contents = x.Contents }).OrderBy(x => x.name).ToList();
                var ind = AttachedEntities.FindIndex(x => x.Equals(change));
                if (ind == -1)
                {
                    set.Attach(change.Contents);
                    AttachedEntities.Add(change);
                    Context.Entry(change.Contents).State = EntityState.Modified;
                }
                else
                {
                    var attached = AttachedEntities[ind];
                    ((IObjectContextAdapter)Context).ObjectContext.Detach(attached.Contents);
                    set.Attach(change.Contents);
                    Context.Entry(change.Contents).State = EntityState.Modified;
                    AttachedEntities[ind] = change;
                }
            }

            Added.Keys.ToList().ForEach(key =>{
                var set = Context.Set(key);
                Added[key].ForEach(entity => set.Add(entity));
            });
        }

        private bool IsList(Type t)
        {
            return t.GetGenericTypeDefinition() == typeof(ICollection<>) || t.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ICollection<>));
        }

        //Returns true if added false if otherwise (attached)
        private Boolean Alter(object entity, Type type)
        {
            bool canNull = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

            if (canNull)
                return false;
            else
            {
                DbEntityEntry dbContents=null;
                var set = Context.Set(type);
                type.GetProperties().ToList().ForEach(property =>
                {
                    var pT = property.PropertyType;
                    var item = property.GetValue(entity);
                    var invalidTypes = new List<Type> { 
                        typeof(String),
                        typeof(DateTime),
                        typeof(TimeSpan),
                        typeof(DateTimeOffset),
                        typeof(Decimal),
                    };

                    if (item != null && !Attribute.IsDefined(pT, typeof(NotMappedAttribute)) && !(pT.IsPrimitive || pT.IsEnum || invalidTypes.Any(invalid => pT == invalid)))
                    {
                        var e = new Entity() { Contents = entity, PrimaryKey = GetKeysValues(entity, type), Type = type };
                        if (!Entities.Contains(e))
                        {
                            if (!IsList(pT))
                            {
                                Entities.Add(e);
                                var added = Alter(item, pT);
                                if (added)
                                    Entities.Remove(e);
                            }
                            else{
                                if (dbContents==null){
                                    dbContents=Context.Entry(entity);
                                }

                                AlterCollection(entity,property, (ICollection<object>)item, item.GetType().GetGenericArguments()[0]);
                            }
                        }
                    }
                });


                if (IsRecoreded(entity, type))
                {
                    var map = new Entity() { Contents = entity, PrimaryKey = GetKeysValues(entity, type), Type = type };
                    var index = ChangedEntities.FindIndex(x => x.Equals(map));
                    if (index != -1)
                        ChangedEntities[index] = map;
                    else
                        ChangedEntities.Add(map);
                    return false;
                }
                else
                {
                    if (!Added.ContainsKey(type))
                        Added[type] = new List<Object>();
                    var setObj = Added[type];
                    var value = setObj.FirstOrDefault(x => x.Equals(entity));
                    if (value != null)
                    {
                        var index = setObj.FindIndex(x => x.Equals(entity));
                        Added[type][index] = entity;
                    }
                    else
                        setObj.Add(SetKeysUnique(entity, type));
                    return true;
                }
            }
        }
        /*
        public void RemoveEntities<T, T2>(T parent,Expression<Func<T, object>> expression, params T2[] children) where T : EntityBase where T2 : EntityBase
        {
            Context.Set<T>().Attach(parent);
            ObjectContext obj = DataContext.ToObjectContext();
            foreach (T2 child in children)
            {
            DataContext.Set<T2>().Attach(child);
            obj.ObjectStateManager.ChangeRelationshipState(parent,
            child, expression, EntityState.Deleted);
            }
            DataContext.SaveChanges();&nbsp;
        }*

        private void AlterCollection(object parent, PropertyInfo parentCollectionProp, ICollection<object> dbValues, ICollection<object> newValues, Type type)
        {
            
            foreach (var item in dbValues.Except(newValues)){
                Expression<Func<object,object>> exp=(x=>parentCollectionProp.GetValue(x));
                ((IObjectContextAdapter)Context).ObjectContext.ObjectStateManager.ChangeRelationshipState(parent,item,exp,EntityState.Deleted);
            }

            foreach (var item in newValues)
                Alter(item, type);
        }

        protected object SetKeysUnique(object entity, Type type)
        {
            var keys = FindPrimaryKeyProperty(Context, type);
            keys.ToList().ForEach(key =>key.SetValue(entity, KeyFromType(key.PropertyType)));
            return entity;
        }

        private object KeyFromType(Type type)
        {
            if (typeof(long) == type || typeof(int) == type)
                return PreviousUnique--;
            else if (typeof(Guid) == type)
            {
                return new Guid(PreviousUnique--, 0, 0, new byte[8]);
            }
            else
                throw new Exception("Unhandled key type. " + type);

        }

        protected object[] GetKeysValues(object entity, Type type)
        {
            return FindPrimaryKeyProperty(Context, type)
                    .Select(key => key.GetValue(entity, null))
                    .ToArray();
        }

        protected Boolean IsRecoreded(object entity, Type type)
        {
            return !FindPrimaryKeyProperty(Context, type).All(key => Equals(key.GetValue(entity, null), key.PropertyType.IsValueType ? Activator.CreateInstance(key.PropertyType) : null));
        }

        private PropertyInfo[] FindPrimaryKeyProperty(IObjectContextAdapter context, Type type)
        {
            if (!KeyDictionary.ContainsKey(type))
            {            //find the primary key
                var objectContext = context.ObjectContext;
                //this will error if it's not a mapped entity

                var method = objectContext.GetType().GetMethod("CreateObjectSet", Type.EmptyTypes);
                var generic = method.MakeGenericMethod(type);
                dynamic objectSet = generic.Invoke(objectContext, null);

                //var objectContext = objectContext.CreateObjectSet<T>();
                var elementType = objectSet.EntitySet.ElementType;
                var list = new List<PropertyInfo>();
                for (int i = 0; i < elementType.KeyMembers.Count; i++)
                {
                    list.Add(type.GetProperty(elementType.KeyMembers[i].Name));
                }
                //look it up on the entity
                KeyDictionary[type] = list.ToArray();
            }
            return KeyDictionary[type];
        }
        

        public void Dispose()
        {
            InternalUpdate();
            if (Added.Any() || ChangedEntities.Any())
                throw new Exception("Some objects were not saved. You should save before exiting the context.");
            Context.Dispose();
        }
    }*/
}