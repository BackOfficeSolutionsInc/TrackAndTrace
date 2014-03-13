using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview
{

    public class ExtraTab
    {
        public class Tab{
            public String Text {get;set;}
            public String Url {get;set;}
            public String Page {get;set;}
        }

        public static List<Tab> Create(params String[] textUrl)
        {
            var output = new List<Tab>();
            for (int i = 0; i < textUrl.Count(); i+=2)
            {
                output.Add(new Tab() { Page = textUrl[i], Text = textUrl[i], Url = textUrl[i + 1] });
            }
            return output;
        }
    }
}