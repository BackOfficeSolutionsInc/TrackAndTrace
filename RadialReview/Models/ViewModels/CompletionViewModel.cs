using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models
{
    public class CompletionModel
    {
        public int RequiredCompleted{get;private set;}
        public int TotalRequired { get; private set; }
        public int OptionalCompleted { get; private set; }
        public int TotalOptional { get; private set; }

        public decimal RequiredPercentage { get; private set; }
        public decimal OptionalPercentage { get; private set; }

        public Boolean FullyComplete { get; private set; }
        public Boolean RequiredComplete { get; private set; }
        public int Completion { get; set; }

        public Boolean Illegal { get; set; }

        private CompletionModel()
        {

        }

        private void Calculate()
        {
            if (TotalRequired != 0)
                RequiredPercentage = RequiredCompleted / (decimal)TotalRequired;
            else
            {
                if (TotalOptional==0)
                    Illegal = true;
                RequiredPercentage = 1m;
            }


            if (TotalOptional != 0)
                OptionalPercentage = OptionalCompleted / (decimal)TotalOptional;
            else
            {
                if (OptionalCompleted > 0)
                    Illegal = true;
                OptionalPercentage = 1m;
            }
            

            if (RequiredCompleted == TotalRequired)
                RequiredPercentage = 1m;
            if (OptionalCompleted == TotalOptional)
                OptionalPercentage = 1m;
            

            RequiredComplete = RequiredPercentage >= 1m;
            FullyComplete = RequiredComplete && OptionalPercentage >= 1m;

            Completion = 0;
            if (RequiredPercentage < 1m)
            {
                Completion = (int)(RequiredPercentage * 100);
            }
            else
            {
                Completion = (int)(OptionalPercentage * 100);
            }
        }

        public CompletionModel(int requiredCompleted,int totalRequired,int optionalCompleted,int totalOptional)
        {
            RequiredCompleted=requiredCompleted;
            TotalRequired=totalRequired;
            OptionalCompleted=optionalCompleted;
            TotalOptional = totalOptional;
            Calculate();
        }

        public CompletionModel(int RequiredCompleted, int TotalRequired):this(RequiredCompleted,TotalRequired,1,1)
        {

        }

        public static CompletionModel operator+(CompletionModel c1,CompletionModel c2)
        {
            var combine = new CompletionModel();
            combine.RequiredCompleted = c1.RequiredCompleted + c2.RequiredCompleted;
            combine.OptionalCompleted = c1.OptionalCompleted + c2.OptionalCompleted;
            combine.TotalRequired = c1.TotalRequired + c2.TotalRequired;
            combine.TotalOptional = c1.TotalOptional + c2.TotalOptional;
            combine.Calculate();

            return combine;
        }

        public static CompletionModel FromList(IEnumerable<CompletionModel> completions)
        {
            var combined=new CompletionModel();

            foreach(var c in completions)
            {
                combined += c;
            }
            return combined;
        }



    }
}