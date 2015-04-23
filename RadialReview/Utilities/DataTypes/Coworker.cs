using RadialReview.Models;
using RadialReview.Models.Askables;
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
	public class CoworkerRelationships : IEnumerable<KeyValuePair<ResponsibilityGroupModel, List<AboutType>>>
    {
		//public OrganizationModel Organization { get; set; }
		public ResponsibilityGroupModel Reviewer { get; set; }
		public Multimap<ResponsibilityGroupModel, AboutType> Relationships { get; set; }

		public CoworkerRelationships(ResponsibilityGroupModel reviewer){
			Reviewer = reviewer;
			Relationships = new Multimap<ResponsibilityGroupModel, AboutType>();
        }

		public void Add(UserOrganizationModel coworker, AboutType relationship){
			Relationships.Add(coworker, relationship);
		}

		public void Add(OrganizationModel coworker){
			Relationships.Add(coworker, AboutType.Organization);
		}


		public IEnumerator<KeyValuePair<ResponsibilityGroupModel, List<AboutType>>> GetEnumerator(){
            return Relationships.GetEnumerator();
        }


        IEnumerator IEnumerable.GetEnumerator(){
            return Relationships.GetEnumerator();
        }
	}
}