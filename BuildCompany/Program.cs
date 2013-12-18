using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BuildCompany
{
    class Program
    {
        static String Website ="localhost:2200/";
        static WebClient Client = new WebClient();
        static void Main(string[] args)
        {
            RegisterUser("Clay","Upton");
        }

        static void RegisterUser(String first,String last)
        {
         
            //Client.UploadString(Website + "Account/Register");
        }
    }
}
