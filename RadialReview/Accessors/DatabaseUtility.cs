using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

namespace RadialReview.Accessors
{
    public static class DatabaseUtility
    {
        /*
        public static void Update<T>(this ApplicationDbContext _context, T entity) where T : class
        {
            if (entity == null)
            {
                throw new ArgumentException("Cannot add a null entity.");
            }

            var entry = _context.Entry<T>(entity);

            // Retreive the Id through reflection

            var set = _context.Set<T>();
            var pkey = set.Create().GetType().GetProperty("Id").GetValue(entity);

            if (entry.State == EntityState.Detached)
            {
                //var set = _context.Set<T>();
                T attachedEntity = set.Find(pkey);  // You need to have access to key
                if (attachedEntity != null){
                    var attachedEntry = _context.Entry(attachedEntity);
                    attachedEntry.CurrentValues.SetValues(entity);
                }else{
                    entry.State = EntityState.Modified; // This should attach entity
                }

                var props = entity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
      
                foreach(var prop in props)
                {
                    var value = prop.GetValue(attachedEntity, null);
                    prop.SetValue(entity, value, null);
                }
            }
        }
         */
    }
}