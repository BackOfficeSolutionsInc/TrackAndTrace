﻿using RadialReview.Models.Responsibilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.ViewModels
{
    public class OrgPositionsViewModel
    {
        public List<OrgPosViewModel> Positions { get; set; }

    }

    public class OrgPosViewModel
    {
        public String Name {get;set;}
        public String SimilarTo { get; set; }
        public int NumAccountabilities { get; set; }
        public int NumPeople { get; set; }

        public OrgPosViewModel(OrganizationPositionModel model,int numPeople)
        {
            Name = model.CustomName;
            SimilarTo = model.Position.Name.Translate();
            NumAccountabilities = model.Responsibilities.Count();
            NumPeople = numPeople;
        }

    }
}