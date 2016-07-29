using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using NHibernate.Engine;
using NHibernate.Envers.Configuration;
using NHibernate.Envers.Configuration.Metadata;
using NHibernate.Envers.Entities.Mapper;
using NHibernate.Envers.Entities.Mapper.Relation;
using NHibernate.Envers.Entities.Mapper.Relation.Query;
using NHibernate.Envers.Synchronization;
using NHibernate.Envers.Tools.Query;
using NHibernate.Type;
using NHibernate;
using NHibernate.Envers;
using NHibernate.Envers.Strategy;
using NHibernate.Transform;
 

namespace RadialReview.Utilities.NHibernate
{
    [Serializable]
    public class CustomValidityAuditStrategy : IAuditStrategy
    {
        private AuditConfiguration _auditConfiguration;

        public void Initialize(AuditConfiguration auditConfiguration)
        {
            _auditConfiguration = auditConfiguration;
        }

        private static DateTime lastFlush = DateTime.MinValue;
        private static DateTime lastCall = DateTime.MinValue;

        public void Perform(ISession session, string entityName, object id, object data, object revision)
        {
            var auditedEntityName = _auditConfiguration.AuditEntCfg.GetAuditEntityName(entityName);

            var reuseEntityIdentifier = _auditConfiguration.GlobalCfg.AllowIdentifierReuse;
            // Update the end date of the previous row if this operation is expected to have a previous row
            var revisionTypeIsAdded = revisionType(_auditConfiguration, data) == RevisionType.Added;
            if (reuseEntityIdentifier || !revisionTypeIsAdded)
            {
                /*
                 Constructing a query:
                 select e from audited_ent e where e.end_rev is null and e.id = :id
                */
                var qb = new QueryBuilder(auditedEntityName, QueryConstants.MiddleEntityAlias);

                // e.id = :id
                var idMapper = _auditConfiguration.EntCfg[entityName].IdMapper;
                idMapper.AddIdEqualsToQuery(qb.RootParameters, id, _auditConfiguration.AuditEntCfg.OriginalIdPropName, true);

                addEndRevisionNullRestriction(_auditConfiguration, qb);

                var l = qb.ToQuery(session)/*.SetLockMode(QueryConstants.MiddleEntityAlias, LockMode.Upgrade)*/.List();

                updateLastRevision(session, _auditConfiguration, l, id, auditedEntityName, revision, (!reuseEntityIdentifier || !revisionTypeIsAdded));
            }

            // Save the audit data
            session.Save(auditedEntityName, data);
            SessionCacheCleaner.ScheduleAuditDataRemoval(session, data);


            //To Speed things along.
            var now = DateTime.UtcNow;
            if (lastCall==DateTime.MinValue){
                lastFlush = now;
            }else{
                if (now - lastCall < TimeSpan.FromSeconds(.1)) {
                    if (now - lastFlush > TimeSpan.FromSeconds(1)) {
                        lastFlush = DateTime.UtcNow;
                        session.Flush();
                        session.Clear();
                    }
                }else{
                    lastFlush = now;
                }
            }
            lastCall = now;

            
        }

        public void PerformCollectionChange(ISession session, string entityName, string propertyName, AuditConfiguration auditCfg, PersistentCollectionChangeData persistentCollectionChangeData, object revision)
        {
            var qb = new QueryBuilder(persistentCollectionChangeData.EntityName, QueryConstants.MiddleEntityAlias);

            var originalIdPropName = _auditConfiguration.AuditEntCfg.OriginalIdPropName;
            var originalId = (IDictionary)persistentCollectionChangeData.Data[originalIdPropName];
            var revisionFieldName = auditCfg.AuditEntCfg.RevisionFieldName;
            var revisionTypePropName = auditCfg.AuditEntCfg.RevisionTypePropName;

            // Adding a parameter for each id component, except the rev number and type.
            foreach (DictionaryEntry originalIdKeyValue in originalId)
            {
                if (!revisionFieldName.Equals(originalIdKeyValue.Key) && !revisionTypePropName.Equals(originalIdKeyValue.Key))
                {
                    qb.RootParameters.AddWhereWithParam(originalIdPropName + "." + originalIdKeyValue.Key, true, "=", originalIdKeyValue.Value);
                }
            }

            var sessionFactory = ((ISessionImplementor)session).Factory;
            var propertyType = sessionFactory.GetEntityPersister(entityName).GetPropertyType(propertyName);
            if (propertyType.IsCollectionType)
            {
                var collectionPropertyType = (CollectionType)propertyType;
                // Handling collection of components.
                if (collectionPropertyType.GetElementType(sessionFactory) is ComponentType)
                {
                    // Adding restrictions to compare data outside of primary key.
                    foreach (var dataEntry in persistentCollectionChangeData.Data)
                    {
                        if (!originalIdPropName.Equals(dataEntry.Key))
                        {
                            qb.RootParameters.AddWhereWithParam(dataEntry.Key, true, "=", dataEntry.Value);
                        }
                    }
                }
            }

            addEndRevisionNullRestriction(_auditConfiguration, qb);

            var l = qb.ToQuery(session).SetLockMode(QueryConstants.MiddleEntityAlias, LockMode.Upgrade).List();

            if (l.Count > 0)
            {
                updateLastRevision(session, _auditConfiguration, l, originalId, persistentCollectionChangeData.EntityName, revision, true);
            }

            // Save the audit data
            var data = persistentCollectionChangeData.Data;
            session.Save(persistentCollectionChangeData.EntityName, data);
            SessionCacheCleaner.ScheduleAuditDataRemoval(session, data);
        }

        private static void addEndRevisionNullRestriction(AuditConfiguration auditCfg, QueryBuilder qb)
        {
            // e.end_rev is null
            qb.RootParameters.AddWhere(auditCfg.AuditEntCfg.RevisionEndFieldName, true, "is", "null", false);
        }

        public void AddEntityAtRevisionRestriction(QueryBuilder rootQueryBuilder, Parameters parameters, string revisionProperty, string revisionEndProperty, bool addAlias, MiddleIdData idData, string revisionPropertyPath, string originalIdPropertyName, string alias1, string alias2)
        {
            addRevisionRestriction(parameters, revisionProperty, revisionEndProperty, addAlias, true);
        }

        public void AddAssociationAtRevisionRestriction(QueryBuilder rootQueryBuilder, Parameters parameters, string revisionProperty, string revisionEndProperty, bool addAlias, MiddleIdData referencingIdData, string versionsMiddleEntityName, string eeOriginalIdPropertyPath, string revisionPropertyPath, string originalIdPropertyName, string alias1, bool inclusive, params MiddleComponentData[] componentDatas)
        {
            addRevisionRestriction(parameters, revisionProperty, revisionEndProperty, addAlias, inclusive);
        }

        /// <summary>
        /// Adds a <![CDATA[<many-to-one>]]> mapping to the revision entity as an endrevision.
        /// Also, if <see cref="AuditEntitiesConfiguration.IsRevisionEndTimestampEnabled"/> set, adds a timestamp when the revision is no longer valid.
        /// </summary>
        public void AddExtraRevisionMapping(XElement classMapping, XElement revisionInfoRelationMapping)
        {
            var verEntCfg = _auditConfiguration.AuditEntCfg;
            var manyToOne = MetadataTools.AddManyToOne(classMapping, verEntCfg.RevisionEndFieldName, verEntCfg.RevisionInfoEntityAssemblyQualifiedName, true, true);
            manyToOne.Add(revisionInfoRelationMapping.Elements());
            MetadataTools.AddOrModifyColumn(manyToOne, verEntCfg.RevisionEndFieldName);

            if (verEntCfg.IsRevisionEndTimestampEnabled)
            {
                const string revisionInfoTimestampSqlType = "Timestamp";
                MetadataTools.AddProperty(classMapping, verEntCfg.RevisionEndTimestampFieldName, revisionInfoTimestampSqlType, true, true, false);
            }
        }

        private static void addRevisionRestriction(Parameters rootParameters, string revisionProperty, string revisionEndProperty, bool addAlias, bool inclusive)
        {
            // e.revision <= _revision and (e.endRevision > _revision or e.endRevision is null)
            var subParm = rootParameters.AddSubParameters("or");
            rootParameters.AddWhereWithNamedParam(revisionProperty, addAlias, inclusive ? "<=" : "<", QueryConstants.RevisionParameter);
            subParm.AddWhereWithNamedParam(revisionEndProperty + ".id", addAlias, inclusive ? ">" : ">=", QueryConstants.RevisionParameter);
            subParm.AddWhere(revisionEndProperty, addAlias, "is", "null", false);
        }

        private static RevisionType revisionType(AuditConfiguration auditCfg, object data)
        {
            return (RevisionType)((IDictionary<string, object>)data)[auditCfg.AuditEntCfg.RevisionTypePropName];
        }

        private void updateLastRevision(ISession session, AuditConfiguration auditCfg, IList l,
                                    object id, string auditedEntityName, object revision, bool throwIfNotOneEntry, bool first = true)
        {
            // There should be one entry
            if (l.Count == 1)
            {
                // Setting the end revision to be the current rev
                var previousData = (IDictionary)l[0];
                var revisionEndFieldName = auditCfg.AuditEntCfg.RevisionEndFieldName;
                previousData[revisionEndFieldName] = revision;

                if (auditCfg.AuditEntCfg.IsRevisionEndTimestampEnabled)
                {
                    // Determine the value of the revision property annotated with @RevisionTimestamp
                    DateTime revisionEndTimestamp;
                    var revEndTimestampFieldName = auditCfg.AuditEntCfg.RevisionEndTimestampFieldName;
                    var revEndTimestampObj = _auditConfiguration.RevisionTimestampGetter.Get(revision);

                    // convert to a DateTime
                    if (revEndTimestampObj is DateTime)
                    {
                        revisionEndTimestamp = (DateTime)revEndTimestampObj;
                    }
                    else
                    {
                        revisionEndTimestamp = new DateTime((long)revEndTimestampObj);
                    }

                    // Setting the end revision timestamp
                    previousData[revEndTimestampFieldName] = revisionEndTimestamp;
                }

                // Saving the previous version
                session.Save(auditedEntityName, previousData);
                SessionCacheCleaner.ScheduleAuditDataRemoval(session, previousData);
            }
            else
            {
                /*if (first == true)
                {
                    //var prefix = auditCfg.AuditEntCfg.;
                    //var suffix = auditCfg.GlobalCfg.ModifiedFlagSuffix;
                    //session.Save(auditedEntityName, session.Get(auditedEntityName.Substring()));
                    //updateLastRevision(session,auditCfg,new List<object>{revision},)
                    var pre_suf = auditCfg.AuditEntCfg.GetAuditEntityName("~!").Split(new string[]{"~!"},StringSplitOptions.None);
                    var pre = pre_suf[0];
                    var suf = pre_suf[1];
                    var entityName = auditedEntityName.Substring(pre.Length, auditedEntityName.Length - (pre.Length + suf.Length));
                    // var previous = session.Get(entityName,id);
                    
                    //////////////////
                    var qb1 = new QueryBuilder(entityName, QueryConstants.MiddleEntityAlias);
                    // e.id = :id
                    var idMapper1 = _auditConfiguration.EntCfg[entityName].IdMapper;
                    idMapper1.AddIdEqualsToQuery(qb1.RootParameters, id,null, true);
                    var lst1 = qb1.ToQuery(session).SetResultTransformer(Transformers.AliasToEntityMap).SetLockMode(QueryConstants.MiddleEntityAlias, LockMode.Upgrade).List();
                    var previous = (IDictionary)lst1[0];
                    //////////

                    session.Save(auditedEntityName, previous);
                    session.Flush();
                    var qb = new QueryBuilder(auditedEntityName, QueryConstants.MiddleEntityAlias);
                    // e.id = :id
                    var idMapper = _auditConfiguration.EntCfg[entityName].IdMapper;
                    idMapper.AddIdEqualsToQuery(qb.RootParameters, id, _auditConfiguration.AuditEntCfg.OriginalIdPropName, true);

                    addEndRevisionNullRestriction(_auditConfiguration, qb);

                    var lst = qb.ToQuery(session).SetLockMode(QueryConstants.MiddleEntityAlias, LockMode.Upgrade).List();

                    updateLastRevision(session, auditCfg, lst, id, auditedEntityName, revision, throwIfNotOneEntry, false);
                }
                else */
                if (throwIfNotOneEntry && false)
                {
                    throw new InvalidOperationException("Cannot find previous revision for entity " + auditedEntityName + " and id " + id);
                }

            }
        }
    }
}