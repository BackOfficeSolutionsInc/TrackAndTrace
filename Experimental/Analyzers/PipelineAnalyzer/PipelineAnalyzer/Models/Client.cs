using ParserUtilities.Utilities.DataTypes;
using ParserUtilities.Utilities.LogFile;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipelineAnalyzer.Models {

	public enum Status {
		Demo,
		Trial,
		Walkthrough,
		Paying,
		Lost,
		Closed,
	}

	public class Client : ILogLine {
		public string Name { get; set; }
		public DateTime EndTime { get { return DateTime.UtcNow; } }
		public DateTime StartTime { get { return GetCreateTime(); } }

		public string Ticket_ID { get; set; }
		public string Ticket_Subject { get; set; }
		public string Created_By { get; set; }
		public string Assigned_Staff_Name { get; set; }
		public string Assigned_Staff_Email { get; set; }
		public string Time_Spent_minutes { get; set; }
		public string Ticket_Status { get; set; }
		public string Status_Behaviour { get; set; }
		public string Ticket_Priority { get; set; }
		public string Ticket_Tags { get; set; }
		public string Ticket_Category { get; set; }
		public string Category_Description { get; set; }
		public string Created_At { get; set; }

		public DateTime GetCreateTime() {
			var date = DateTime.MinValue;
			if (!DateTime.TryParseExact(Created_At, "dd/MM/yy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
				throw new Exception("Bad date format");
			return date;
		}

		public double GetWeeksAgo() {
			return (DateTime.UtcNow - GetCreateTime()).TotalDays / 7.0;
		}

		public string Last_Updated_At { get; set; }
		public string Last_Staff_Reply_At { get; set; }
		public string Last_User_Reply_At { get; set; }
		public string Due_Date { get; set; }
		public string Response_Time_Minutes { get; set; }
		public string First_Response_Time_Minutes { get; set; }
		public string Last_Closed_At { get; set; }
		public string Last_Closed_By { get; set; }
		public string num_staff_replies { get; set; }
		public string num_staff_private_notes { get; set; }
		public string num_client_replies { get; set; }
		public string Contact_Name { get; set; }
		public string Contact_Email { get; set; }
		public string Mobile { get; set; }
		public string Work { get; set; }
		public string Main { get; set; }
		public string Home { get; set; }
		public string Other { get; set; }
		public string Did_Client_Call_In { get; set; }
		public string TestCategory { get; set; }
		public string TestSubCategory { get; set; }
		public string Trial_End_Date { get; set; }
		public string Subcategory { get; set; }
		public string Subcategory_Details { get; set; }
		public string Optional_Contact_Fields { get; set; }
		public string Account_has_been_linked_to_EOSI_or_Coach_per_verified_information { get; set; }
		public string Mobile_Phone_Number { get; set; }
		public string Primary_Lead_Source { get; set; }
		public string Client_Referral_Detail { get; set; }
		public string Digital_Detail { get; set; }
		public string LinkedIn_Detail { get; set; }
		public string Facebook_Detail { get; set; }
		public string Other_Options { get; set; }
		public string EO_Group_Referral_Detail { get; set; }
		public string Other_Detail { get; set; }
		public string Event_Detail { get; set; }
		public string Peer_Group__Coaching_Group_Detail { get; set; }
		public string Coach_Referral_Name { get; set; }
		public string Other_Coach_Name_Detail { get; set; }
		public string EOSI_Referral_Name { get; set; }
		public string Lead_Source_Deprecating { get; set; }
		public string Business_Conference_Referral_Deprecating { get; set; }
		public string Other_Business_Conference_Referral_Deprecating { get; set; }
		public string Other_Lead_Source_Deprecating { get; set; }
		public string EOSBusiness_Conference_Detail_Deprecating { get; set; }
		public string Other_Traction_Forum_Deprecating { get; set; }
		public string Outbound_Marketing_Detail_Deprecating { get; set; }
		public string Other_Outbound_Marketing_Detail_Deprecating { get; set; }
		public string Referral_Source_Deprecating { get; set; }
		public string Is_an_EOSI { get; set; }
		public string Has_an_EOSI_or_Coach { get; set; }
		public string Is_Self_Implementing { get; set; }
		public string Link_Traction_Tools_Account_to_this_EOSI { get; set; }
		public string Link_Traction_Tools_Account_to_this_Coach { get; set; }
		public string Account_Link_to_Coach_List { get; set; }
		public string Account_Link_to_EOSI_List { get; set; }
		public string Company_Size { get; set; }
		public string Title { get; set; }
		public string Trial_End_Date2 { get; set; }
		public string Lost_Account_Detail { get; set; }
		public string Apps_Used { get; set; }
		public string Trial_Email_Status_Marketing { get; set; }
		public string EOSI_or_Coach_Referral_Code { get; set; }
		public string Close_Criteria_Meetings { get; set; }
		public string Org_Flags { get; set; }
		public string Last_Login_Org { get; set; }
		public string Notes { get; set; }
		public string Lead_Source_Notes { get; set; }
		public string Website { get; set; }
		public string Country { get; set; }
		public string City { get; set; }
		public string State { get; set; }
		public string Street_Address { get; set; }
		public string Zip { get; set; }
		public string Phone_Number { get; set; }
		public string Company_Name { get; set; }



		public string[] GetHeaders() {
			return new string[] {
				"Ticket ID",
				"Ticket Subject",
				"Created By",
				"Assigned Staff Name",
				"Assigned Staff Email",
				"Time Spent (minutes)",
				"Ticket Status",
				"Status Behaviour",
				"Ticket Priority",
				"Ticket Tags",
				"Ticket Category",
				"Category Description",
				"Created At",
				"Last Updated At",
				"Last Staff Reply At",
				"Last User Reply At",
				"Due Date",
				"Response Time (Minutes)",
				"First Response Time (Minutes)",
				"Last Closed At",
				"Last Closed By",
				"# staff replies",
				"# staff private notes",
				"# client replies",
				"Contact Name",
				"Contact Email",
				"Mobile",
				"Work",
				"Main",
				"Home",
				"Other",
				"Did Client Call In?",
				"TestCategory",
				"TestSubCategory",
				"Trial End Date",
				"Subcategory",
				"Subcategory Details",
				"Optional Contact Fields",
				"Account has been linked to EOSI or Coach per verified information?",
				"Mobile Phone Number",
				"Primary Lead Source",
				"Client Referral Detail",
				"Digital Detail",
				"LinkedIn Detail",
				"Facebook Detail",
				"Other Options",
				"EO Group Referral Detail",
				"Other Detail",
				"Event Detail",
				"Peer Group / Coaching Group Detail",
				"Coach Referral Name",
				"Other Coach Name Detail",
				"EOSI Referral Name",
				"Lead Source (Deprecating)",
				"Business Conference Referral (Deprecating)",
				"Other Business Conference Referral (Deprecating)",
				"Other Lead Source (Deprecating)",
				"EOS/Business Conference Detail (Deprecating)",
				"Other Traction Forum (Deprecating)",
				"Outbound Marketing Detail (Deprecating)",
				"Other Outbound Marketing Detail (Deprecating)",
				"Referral Source (Deprecating)",
				"Is an EOSI",
				"Has an EOSI or Coach",
				"Is Self-Implementing",
				"Link Traction Tools Account to this EOSI?",
				"Link Traction Tools Account to this Coach?",
				"Account Link to Coach List",
				"Account Link to EOSI List",
				"Company Size",
				"Title",
				"Trial End Date",
				"Lost Account Detail",
				"Apps Used",
				"Trial Email Status (Marketing)",
				"EOSI or Coach Referral Code",
				"Close Criteria Meetings",
				"Org Flags",
				"Last Login Org",
				"Notes",
				"Lead Source Notes",
				"Website",
				"Country",
				"City",
				"State",
				"Street Address",
				"Zip",
				"Phone Number",
				"Company Name"
			};
		}

		public ILogLine ConstructFromLine(string linex) {
			var line = Csv.LineToCells(linex);
			return new Client() {
				Ticket_ID = line[0],
				Ticket_Subject = line[1],
				Created_By = line[2],
				Assigned_Staff_Name = line[3],
				Assigned_Staff_Email = line[4],
				Time_Spent_minutes = line[5],
				Ticket_Status = line[6],
				Status_Behaviour = line[7],
				Ticket_Priority = line[8],
				Ticket_Tags = line[9],
				Ticket_Category = line[10],
				Category_Description = line[11],
				Created_At = line[12],
				Last_Updated_At = line[13],
				Last_Staff_Reply_At = line[14],
				Last_User_Reply_At = line[15],
				Due_Date = line[16],
				Response_Time_Minutes = line[17],
				First_Response_Time_Minutes = line[18],
				Last_Closed_At = line[19],
				Last_Closed_By = line[20],
				num_staff_replies = line[21],
				num_staff_private_notes = line[22],
				num_client_replies = line[23],
				Contact_Name = line[24],
				Contact_Email = line[25],
				Mobile = line[26],
				Work = line[27],
				Main = line[28],
				Home = line[29],
				Other = line[30],
				Did_Client_Call_In = line[31],
				TestCategory = line[32],
				TestSubCategory = line[33],
				Trial_End_Date = line[34],
				Subcategory = line[35],
				Subcategory_Details = line[36],
				Optional_Contact_Fields = line[37],
				Account_has_been_linked_to_EOSI_or_Coach_per_verified_information = line[38],
				Mobile_Phone_Number = line[39],
				Primary_Lead_Source = line[40],
				Client_Referral_Detail = line[41],
				Digital_Detail = line[42],
				LinkedIn_Detail = line[43],
				Facebook_Detail = line[44],
				Other_Options = line[45],
				EO_Group_Referral_Detail = line[46],
				Other_Detail = line[47],
				Event_Detail = line[48],
				Peer_Group__Coaching_Group_Detail = line[49],
				Coach_Referral_Name = line[50],
				Other_Coach_Name_Detail = line[51],
				EOSI_Referral_Name = line[52],
				Lead_Source_Deprecating = line[53],
				Business_Conference_Referral_Deprecating = line[54],
				Other_Business_Conference_Referral_Deprecating = line[55],
				Other_Lead_Source_Deprecating = line[56],
				EOSBusiness_Conference_Detail_Deprecating = line[57],
				Other_Traction_Forum_Deprecating = line[58],
				Outbound_Marketing_Detail_Deprecating = line[59],
				Other_Outbound_Marketing_Detail_Deprecating = line[60],
				Referral_Source_Deprecating = line[61],
				Is_an_EOSI = line[62],
				Has_an_EOSI_or_Coach = line[63],
				Is_Self_Implementing = line[64],
				Link_Traction_Tools_Account_to_this_EOSI = line[65],
				Link_Traction_Tools_Account_to_this_Coach = line[66],
				Account_Link_to_Coach_List = line[67],
				Account_Link_to_EOSI_List = line[68],
				Company_Size = line[69],
				Title = line[70],
				Trial_End_Date2 = line[71],
				Lost_Account_Detail = line[72],
				Apps_Used = line[73],
				Trial_Email_Status_Marketing = line[74],
				EOSI_or_Coach_Referral_Code = line[75],
				Close_Criteria_Meetings = line[76],
				Org_Flags = line[77],
				Last_Login_Org = line[78],
				Notes = line[79],
				Lead_Source_Notes = line[80],
				Website = line[81],
				Country = line[82],
				City = line[83],
				State = line[84],
				Street_Address = line[85],
				Zip = line[86],
				Phone_Number = line[87],
				Company_Name = line[88],
			};

		}

		public string[] GetLine(DateTime firstLogStartTime) {
			return new[] {
				Ticket_ID                                                                  ,
				Ticket_Subject                                                             ,
				Created_By                                                                 ,
				Assigned_Staff_Name                                                        ,
				Assigned_Staff_Email                                                       ,
				Time_Spent_minutes                                                         ,
				Ticket_Status                                                              ,
				Status_Behaviour                                                           ,
				Ticket_Priority                                                            ,
				Ticket_Tags                                                                ,
				Ticket_Category                                                            ,
				Category_Description                                                       ,
				Created_At                                                                 ,
				Last_Updated_At                                                            ,
				Last_Staff_Reply_At                                                        ,
				Last_User_Reply_At                                                         ,
				Due_Date                                                                   ,
				Response_Time_Minutes                                                      ,
				First_Response_Time_Minutes                                                ,
				Last_Closed_At                                                             ,
				Last_Closed_By                                                             ,
				num_staff_replies                                                          ,
				num_staff_private_notes                                                    ,
				num_client_replies                                                         ,
				Contact_Name                                                               ,
				Contact_Email                                                              ,
				Mobile                                                                     ,
				Work                                                                       ,
				Main                                                                       ,
				Home                                                                       ,
				Other                                                                      ,
				Did_Client_Call_In                                                         ,
				TestCategory                                                               ,
				TestSubCategory                                                            ,
				Trial_End_Date                                                             ,
				Subcategory                                                                ,
				Subcategory_Details                                                        ,
				Optional_Contact_Fields                                                    ,
				Account_has_been_linked_to_EOSI_or_Coach_per_verified_information          ,
				Mobile_Phone_Number                                                        ,
				Primary_Lead_Source                                                        ,
				Client_Referral_Detail                                                     ,
				Digital_Detail                                                             ,
				LinkedIn_Detail                                                            ,
				Facebook_Detail                                                            ,
				Other_Options                                                              ,
				EO_Group_Referral_Detail                                                   ,
				Other_Detail                                                               ,
				Event_Detail                                                               ,
				Peer_Group__Coaching_Group_Detail                                          ,
				Coach_Referral_Name                                                        ,
				Other_Coach_Name_Detail                                                    ,
				EOSI_Referral_Name                                                         ,
				Lead_Source_Deprecating                                                    ,
				Business_Conference_Referral_Deprecating                                   ,
				Other_Business_Conference_Referral_Deprecating                             ,
				Other_Lead_Source_Deprecating                                              ,
				EOSBusiness_Conference_Detail_Deprecating                                  ,
				Other_Traction_Forum_Deprecating                                           ,
				Outbound_Marketing_Detail_Deprecating                                      ,
				Other_Outbound_Marketing_Detail_Deprecating                                ,
				Referral_Source_Deprecating                                                ,
				Is_an_EOSI                                                                 ,
				Has_an_EOSI_or_Coach                                                       ,
				Is_Self_Implementing                                                       ,
				Link_Traction_Tools_Account_to_this_EOSI                                   ,
				Link_Traction_Tools_Account_to_this_Coach                                  ,
				Account_Link_to_Coach_List                                                 ,
				Account_Link_to_EOSI_List                                                  ,
				Company_Size                                                               ,
				Title                                                                      ,
				Trial_End_Date                                                             ,
				Lost_Account_Detail                                                        ,
				Apps_Used                                                                  ,
				Trial_Email_Status_Marketing                                               ,
				EOSI_or_Coach_Referral_Code                                                ,
				Close_Criteria_Meetings                                                    ,
				Org_Flags                                                                  ,
				Last_Login_Org                                                             ,
				Notes                                                                      ,
				Lead_Source_Notes                                                          ,
				Website                                                                    ,
				Country                                                                    ,
				City                                                                       ,
				State                                                                      ,
				Street_Address                                                             ,
				Zip                                                                        ,
				Phone_Number                                                               ,
				Company_Name
			};
		}
	}
}
