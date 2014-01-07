using RadialReview.Models;
using RadialReview.Models.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.DataTypes
{/*
    public class Relationships
    {
        public UserOrganizationModel Coworker { get; set; }
        public List<AboutType> Relationship { get; set; }

        public Relationships()
        {
            Relationship = new List<AboutType>();
        }
    }
    */
    public class CoworkerRelationships : IEnumerable<KeyValuePair<UserOrganizationModel,List<AboutType>>>
    {
        public UserOrganizationModel User { get; set; }
        public Multimap<UserOrganizationModel, AboutType> Relationships { get; set; }

        public CoworkerRelationships(UserOrganizationModel user)
        {
            User = user;
            Relationships = new Multimap<UserOrganizationModel, AboutType>();

        }

        public void Add(UserOrganizationModel coworker, AboutType relationship)
        {
            Relationships.Add(coworker, relationship);
        }


        public IEnumerator<KeyValuePair<UserOrganizationModel,List<AboutType>>> GetEnumerator()
        {
            return Relationships.GetEnumerator();
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return Relationships.GetEnumerator();
        }
    }
}