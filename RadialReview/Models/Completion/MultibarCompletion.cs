using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models
{
    public class MultibarCompletion : ICompletionModel
    {
        public List<CompletionModel> Completions { get; set; }

        public List<CompletionModel> GetCompletions()
        {
            return Completions;
        }

        public MultibarCompletion(IEnumerable<CompletionModel> completions)
        {
            Completions = completions.ToList();
        }

        public bool FullyComplete
        {
            get {
                var fullyComplete = true;
                foreach (var c in Completions)
                {
                    fullyComplete = (c.FullyComplete && fullyComplete);
                }
                return fullyComplete;
            }
        }

        public bool Started
        {
            get {
                var started = false;
                foreach (var c in Completions)
                {
                    started = (c.Started || started);
                }
                return started;
            }
        }


        public bool Illegal
        {
            get
            {
                var anyIllegal = false;
                foreach (var c in Completions)
                {
                    anyIllegal = (c.Illegal || anyIllegal);
                }
                return anyIllegal;
            }
        }
    }
    /*
    public class CompletionItem{
        public decimal Count {get;set;}
        public decimal Complete {get;set;}
        public String Title { get; set; }
        public String Class { get; set; }

        public int GetPercentage()
        {
            return (int)(100.0m * Complete / Count);
        }
    }*/
}