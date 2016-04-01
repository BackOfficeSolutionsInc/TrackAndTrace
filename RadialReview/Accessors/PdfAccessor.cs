using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
//using Pechkin;
//using Pechkin.Synchronized;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using NHibernate;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.L10;
using RadialReview.Models.Pdf;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;
using System.Xml.XPath;
using RadialReview.Models.Angular.Meeting;
using System.Reflection;
using RadialReview.Properties;

namespace RadialReview.Accessors
{
    public class LayoutHelper
    {
        private readonly PdfDocument _document;
        private readonly XUnit _topPosition;
        private readonly XUnit _bottomMargin;
        private XUnit _currentPosition;
        public LayoutHelper(PdfDocument document, XUnit topPosition, XUnit bottomMargin)
        {
            _document = document;
            _topPosition = topPosition;
            _bottomMargin = bottomMargin;
            // Set a value outside the page - a new page will be created on the first request.
            _currentPosition = bottomMargin + 10000;
        }
        public XUnit GetLinePosition(XUnit requestedHeight)
        {
            return GetLinePosition(requestedHeight, -1f);
        }
        public XUnit GetLinePosition(XUnit requestedHeight, XUnit requiredHeight)
        {
            XUnit required = requiredHeight == -1f ? requestedHeight : requiredHeight;
            if (_currentPosition + required > _bottomMargin)
                CreatePage();
            XUnit result = _currentPosition;
            _currentPosition += requestedHeight;
            return result;
        }
        public XGraphics Gfx { get; private set; }
        public PdfPage Page { get; private set; }

        void CreatePage()
        {
            Page = _document.AddPage();
            Page.Size = PageSize.A4;
            Gfx = XGraphics.FromPdfPage(Page);
            _currentPosition = _topPosition;
        }
    }


    public class PdfAccessor
    {
        public static Document CreateDoc(UserOrganizationModel caller, string docTitle)
        {
            var document = new Document();



            document.Info.Title = docTitle;
            document.Info.Author = caller.GetName();
            document.Info.Comment = "Created with Traction® Tools";


            //document.DefaultPageSetup.PageFormat = PageFormat.Letter;
            document.DefaultPageSetup.Orientation = Orientation.Portrait;

            document.DefaultPageSetup.LeftMargin = Unit.FromInch(.5);
            document.DefaultPageSetup.RightMargin = Unit.FromInch(.5);
            document.DefaultPageSetup.TopMargin = Unit.FromInch(.5);
            document.DefaultPageSetup.BottomMargin = Unit.FromInch(.5);
            document.DefaultPageSetup.PageWidth = Unit.FromInch(8.5);
            document.DefaultPageSetup.PageHeight = Unit.FromInch(11);


            return document;
        }

        private static Color TableGray = new Color(100, 100, 100, 100);
        private static Color TableBlack = new Color(0, 0, 0);

        protected static Section AddTitledPage(Document document, string pageTitle, Orientation orientation = Orientation.Portrait,bool addSection=true)
        {
            Section section;

            if (addSection||document.LastSection==null) {
                section = document.AddSection();
                section.PageSetup.Orientation = orientation;
            } else {
                section = document.LastSection;
            }

            var paragraph = new Paragraph();
            paragraph.AddTab();
            paragraph.AddPageField();
            // Add paragraph to footer for odd pages.
            section.Footers.Primary.Add(paragraph);
            section.Footers.Primary.Format.SpaceBefore = Unit.FromInch(-.25);


            var frame = section.AddTextFrame();
            frame.Height = Unit.FromInch(.75);
            frame.Width = Unit.FromInch(10);
            //frame.Left = ShapePosition.Center;

            frame.MarginRight = Unit.FromInch(1);
            frame.MarginLeft = Unit.FromInch(1);

            var title = frame.AddTable();
            title.Borders.Color = TableBlack;

            var size = Unit.FromInch(5);
            if (orientation == Orientation.Landscape)
                size = Unit.FromInch(8);
            var c = title.AddColumn(size);
            c.Format.Alignment = ParagraphAlignment.Center;
            var rr = title.AddRow();
            rr.Cells[0].AddParagraph(pageTitle);
            rr.Format.Font.Bold = true;
            rr.Shading.Color = TableGray;
            rr.HeightRule = RowHeightRule.AtLeast;
            rr.VerticalAlignment = VerticalAlignment.Center;
            rr.Height = Unit.FromInch(0.4);
            rr.Format.Font.Size = Unit.FromInch(.2);

            return section;
        }
        public static void AddTodos(UserOrganizationModel caller, Document doc, AngularRecurrence recur)
        {
            //var recur = L10Accessor.GetAngularRecurrence(caller, recurrenceId);

            //return SetupDoc(caller, caller.Organization.Settings.RockName);

            var section = AddTitledPage(doc, "To-do List");

            var table = section.AddTable();
            table.Style = "Table";
            table.Rows.LeftIndent = 0;
            table.LeftPadding = 0;
            table.RightPadding = 0;

            //Number
            var column = table.AddColumn(Unit.FromInch(/*0.2*/0));
            column.Format.Alignment = ParagraphAlignment.Center;

            //Due
            column = table.AddColumn(Unit.FromInch(0.7));
            column.Format.Alignment = ParagraphAlignment.Center;

            //Who
            column = table.AddColumn(Unit.FromInch(1+0.2));
            column.Format.Alignment = ParagraphAlignment.Center;


            //Rock
            column = table.AddColumn(Unit.FromInch(4.85 + .75));
            column.Format.Alignment = ParagraphAlignment.Left;

            var row = table.AddRow();
            row.HeadingFormat = true;
            row.Format.Alignment = ParagraphAlignment.Center;
            row.Format.Font.Bold = true;
            row.Shading.Color = TableGray;
            row.Height = Unit.FromInch(0.25);

            row.Cells[1].AddParagraph("Due");
            row.Cells[1].VerticalAlignment = VerticalAlignment.Bottom;

            row.Cells[2].AddParagraph("Owner");
            row.Cells[2].VerticalAlignment = VerticalAlignment.Bottom;

            row.Cells[3].AddParagraph("To-dos");
            row.Cells[3].VerticalAlignment = VerticalAlignment.Bottom;
            row.Cells[3].Format.Alignment = ParagraphAlignment.Left;

            var mn = 1;
            foreach (var m in recur.Todos.Where(x => x.Complete == false).OrderBy(x => x.Owner.Name).ThenBy(x => x.DueDate))
            {

                row = table.AddRow();
                row.HeadingFormat = false;
                row.Format.Alignment = ParagraphAlignment.Center;

                row.Format.Font.Bold = false;
                row.Format.Font.Size = Unit.FromInch(0.128 * 2.0 / 3.0); // --- 1/16"
                //row.Shading.Color = TableBlue;
                row.HeightRule = RowHeightRule.AtLeast;
                row.VerticalAlignment = VerticalAlignment.Center;
                row.Height = Unit.FromInch((6 * 8 + 5.0) / (8 * 16.0) / 2);
                //row.Cells[0].AddParagraph("" + mn + ".");
                row.Cells[1].AddParagraph(m.DueDate.NotNull(x => x.Value.ToShortDateString()) ?? "Not-set");
                row.Cells[2].AddParagraph("" + m.Owner.NotNull(x => x.Name));
                row.Cells[2].Format.Alignment = ParagraphAlignment.Center;
                row.Cells[3].AddParagraph(m.Name);
                row.Cells[3].Format.Alignment = ParagraphAlignment.Left;
                mn++;
            }
        }

        public static void AddIssues(UserOrganizationModel caller, Document doc, AngularRecurrence recur,bool mergeWithTodos)
        {
            //var recur = L10Accessor.GetAngularRecurrence(caller, recurrenceId);

            //return SetupDoc(caller, caller.Organization.Settings.RockName);

            var section = AddTitledPage(doc, "Issues List", addSection:!mergeWithTodos);

            var table = section.AddTable();
            table.Style = "Table";
            table.Rows.LeftIndent = 0;
            table.LeftPadding = 0;
            table.RightPadding = 0;

            //Number
            var column = table.AddColumn(Unit.FromInch(/*0.2*/0));
            column.Format.Alignment = ParagraphAlignment.Center;

            //Priority
            var size = Unit.FromInch(0.0);
            var isPriority = recur.Prioritization==PrioritizationType.Priority;
            if (isPriority)
                size = Unit.FromInch(0.7);
            column = table.AddColumn(size);
            column.Format.Alignment = ParagraphAlignment.Center;

            //Who
            column = table.AddColumn(Unit.FromInch(1));
            column.Format.Alignment = ParagraphAlignment.Center;


            //Issue
            column = table.AddColumn(Unit.FromInch(4.85 + .75 + (isPriority?0:.7)+0.2));
            column.Format.Alignment = ParagraphAlignment.Left;

            var row = table.AddRow();
            row.HeadingFormat = true;
            row.Format.Alignment = ParagraphAlignment.Center;
            row.Format.Font.Bold = true;
            row.Shading.Color = TableGray;
            row.Height = Unit.FromInch(0.25);

            row.Cells[1].AddParagraph(isPriority?"Priority":"");
            row.Cells[1].VerticalAlignment = VerticalAlignment.Bottom;

            row.Cells[2].AddParagraph("Owner");
            row.Cells[2].VerticalAlignment = VerticalAlignment.Bottom;

            row.Cells[3].AddParagraph("Issue");
            row.Cells[3].VerticalAlignment = VerticalAlignment.Bottom;

            var mn = 1;
            foreach (var m in recur.Issues.Where(x => x.Complete == false).OrderByDescending(x => x.Priority).ThenBy(x => x.Name))
            {

                row = table.AddRow();
                row.HeadingFormat = false;
                row.Format.Alignment = ParagraphAlignment.Center;

                row.Format.Font.Bold = false;
                row.Format.Font.Size = Unit.FromInch(0.128 * 2.0 / 3.0); // --- 1/16"
                //row.Shading.Color = TableBlue;
                row.HeightRule = RowHeightRule.AtLeast;
                row.VerticalAlignment = VerticalAlignment.Center;
                row.Height = Unit.FromInch((6 * 8 + 5.0) / (8 * 16.0) / 2);
                //row.Cells[0].AddParagraph("" + mn + ".");

                var p = "";
                if (isPriority) {
                    if (m.Priority >= 1 && m.Priority <= 3) {
                        for (var i = 0; i < m.Priority; i++)
                            p += "*";//"★";
                    } else if (m.Priority > 3) {
                        p = "* x" + m.Priority;
                    }
                    //row.Cells[1].Format.Font.Name = "Arial";
                    row.Cells[1].AddParagraph(p);
                }

                //if (m.Priority >= 1)
                //{
                //    var location = System.Reflection.Assembly.GetExecutingAssembly().Location + "\\..\\..\\Resources\\Star.png";
                //    row.Cells[1].AddImage(location);
                //    row.Cells[1].AddParagraph(" x"+m.Priority);
                //}

                row.Cells[2].AddParagraph("" + m.Owner.NotNull(x => x.Name));
                row.Cells[2].Format.Alignment = ParagraphAlignment.Center;
                row.Cells[3].AddParagraph(m.Name);
                row.Cells[3].Format.Alignment = ParagraphAlignment.Left;
                mn++;
            }
        }

        public static void AddRocks(UserOrganizationModel caller, Document doc, AngularRecurrence recur)
        {
            //var recur = L10Accessor.GetAngularRecurrence(caller, recurrenceId);

            //return SetupDoc(caller, caller.Organization.Settings.RockName);

            var section = AddTitledPage(doc, "Quarterly "+caller.Organization.Settings.RockName, Orientation.Landscape);
            Table table;
            double mult;
            Row row;
            int mn;
            Column column;

            if (recur.Rocks.Any(x => x.CompanyRock ?? false))
            {
                table = section.AddTable();
                table.Style = "Table";
                table.Rows.LeftIndent = Unit.FromInch(1.25);
                
                table.LeftPadding = 0;
                table.RightPadding = 0;

                table.Format.Alignment = ParagraphAlignment.Center;

                mult = 1.0;
                //Number
                column = table.AddColumn(Unit.FromInch(/*0.2*/0 * mult));
                column.Format.Alignment = ParagraphAlignment.Center;
                //Due
                column = table.AddColumn(Unit.FromInch(0.7 * mult));
                column.Format.Alignment = ParagraphAlignment.Center;
                //Who
                column = table.AddColumn(Unit.FromInch(1+.2 * mult));
                column.Format.Alignment = ParagraphAlignment.Center;
                //Completion
                column = table.AddColumn(Unit.FromInch(0.75 * mult));
                column.Format.Alignment = ParagraphAlignment.Center;
                //Rock
                column = table.AddColumn(Unit.FromInch(4.85 * mult));
                column.Format.Alignment = ParagraphAlignment.Left;

                row = table.AddRow();
                row.HeadingFormat = true;
                row.Format.Alignment = ParagraphAlignment.Center;
                row.Format.Font.Bold = true;
                row.Shading.Color = TableGray;
                row.Height = Unit.FromInch(0.25);

                row.Cells[1].AddParagraph("Due");
                row.Cells[1].VerticalAlignment = VerticalAlignment.Bottom;

                row.Cells[2].AddParagraph("Owner");
                row.Cells[2].VerticalAlignment = VerticalAlignment.Bottom;

                row.Cells[3].AddParagraph("Status");
                row.Cells[3].VerticalAlignment = VerticalAlignment.Bottom;

                row.Cells[4].AddParagraph("Company Rock");
                row.Cells[4].VerticalAlignment = VerticalAlignment.Bottom;

                mn = 1;
                foreach (var m in recur.Rocks.Where(x => x.CompanyRock == true).OrderBy(x => x.Owner.Name).ThenBy(x => x.DueDate))
                {

                    row = table.AddRow();
                    row.HeadingFormat = false;
                    row.Format.Alignment = ParagraphAlignment.Center;

                    row.Format.Font.Bold = false;
                    row.Format.Font.Size = Unit.FromInch(0.128 * 2.0 / 3.0); // --- 1/16"
                    //row.Shading.Color = TableBlue;
                    row.HeightRule = RowHeightRule.AtLeast;
                    row.VerticalAlignment = VerticalAlignment.Center;
                    row.Height = Unit.FromInch((6 * 8 + 5.0) / (8 * 16.0) / 2);
                    //row.Cells[0].AddParagraph("" + mn + ".");
                    row.Cells[1].AddParagraph(m.DueDate.NotNull(x => x.Value.ToShortDateString()) ?? "Not-set");
                    row.Cells[2].AddParagraph("" + m.Owner.NotNull(x => x.Name));
                    row.Cells[2].Format.Alignment = ParagraphAlignment.Center;
                    row.Cells[3].AddParagraph("" + m.Completion.NotNull(x => x.Value.GetDisplayName()));
                    row.Cells[3].Format.Font.Bold = m.Completion == RockState.AtRisk;
                    row.Cells[3].Format.Alignment = ParagraphAlignment.Center;
                    row.Cells[4].AddParagraph(m.Name);
                    row.Cells[4].Format.Alignment = ParagraphAlignment.Left;
                    mn++;
                }
                row = table.AddRow();
                row.HeightRule = RowHeightRule.AtLeast;
                row.Height = Unit.FromInch((6 * 8 + 5.0) / (8 * 16.0));
            }
            table = section.AddTable();
            table.Style = "Table";
            table.Rows.LeftIndent = 0;
            table.LeftPadding = 0;
            table.RightPadding = 0;


            mult = 10.0 / 7.5;
            //Number
            column = table.AddColumn(Unit.FromInch(/*0.2*/ 0 * mult));
            column.Format.Alignment = ParagraphAlignment.Center;
            //Due
            column = table.AddColumn(Unit.FromInch(0.7 * mult));
            column.Format.Alignment = ParagraphAlignment.Center;
            //Who
            column = table.AddColumn(Unit.FromInch(1+0.2 * mult));
            column.Format.Alignment = ParagraphAlignment.Center;
            //Completion
            column = table.AddColumn(Unit.FromInch(0.75 * mult));
            column.Format.Alignment = ParagraphAlignment.Center;
            //Rock
            column = table.AddColumn(Unit.FromInch(4.85 * mult));
            column.Format.Alignment = ParagraphAlignment.Left;

            row = table.AddRow();
            row.HeadingFormat = true;
            row.Format.Alignment = ParagraphAlignment.Center;
            row.Format.Font.Bold = true;
            row.Shading.Color = TableGray;
            row.Height = Unit.FromInch(0.25);

            row.Cells[1].AddParagraph("Due");
            row.Cells[1].VerticalAlignment = VerticalAlignment.Bottom;

            row.Cells[2].AddParagraph("Owner");
            row.Cells[2].VerticalAlignment = VerticalAlignment.Bottom;

            row.Cells[3].AddParagraph("Status");
            row.Cells[3].VerticalAlignment = VerticalAlignment.Bottom;

            row.Cells[4].AddParagraph("Rock");
            row.Cells[4].VerticalAlignment = VerticalAlignment.Bottom;

            mn = 1;
            foreach (var m in recur.Rocks.OrderBy(x => x.Owner.Name).ThenBy(x => x.DueDate))
            {

                row = table.AddRow();
                row.HeadingFormat = false;
                row.Format.Alignment = ParagraphAlignment.Center;

                row.Format.Font.Bold = false;
                row.Format.Font.Size = Unit.FromInch(0.128 * 2.0 / 3.0); // --- 1/16"
                //row.Shading.Color = TableBlue;
                row.HeightRule = RowHeightRule.AtLeast;
                row.VerticalAlignment = VerticalAlignment.Center;
                row.Height = Unit.FromInch((6 * 8 + 5.0) / (8 * 16.0) / 2);
                //row.Cells[0].AddParagraph("" + mn + ".");
                row.Cells[1].AddParagraph(m.DueDate.NotNull(x => x.Value.ToShortDateString()) ?? "Not-set");
                row.Cells[2].AddParagraph("" + m.Owner.NotNull(x => x.Name));
                row.Cells[2].Format.Alignment = ParagraphAlignment.Center;
                row.Cells[3].AddParagraph("" + m.Completion.NotNull(x => x.Value.GetDisplayName()));
                row.Cells[3].Format.Font.Bold = m.Completion == RockState.AtRisk;
                row.Cells[3].Format.Alignment = ParagraphAlignment.Center;
                row.Cells[4].AddParagraph(m.Name);
                row.Cells[4].Format.Alignment = ParagraphAlignment.Left;
                mn++;
            }
        }

        public static void AddScorecard(Document doc, AngularRecurrence recur)
        {
            // Create a new PDF document
            //var recur = L10Accessor.GetAngularRecurrence(caller,recurrenceId);


            // var document = SetupDoc(caller, "Scorecard", Orientation.Landscape);

            var section = AddTitledPage(doc, "Scorecard", Orientation.Landscape);


            var TableGray = new Color(100, 100, 100, 100);
            var TableBlack = new Color(0, 0, 0);

            // var section = document.AddSection();



            var table = section.AddTable();
            table.Style = "Table";
            table.Borders.Color = TableBlack;
            table.Borders.Width = 1;
            /*table.Borders.Left.Width = 0.25;
            table.Borders.Right.Width = 0.25;
            table.Borders.Top.Width = 7.0/8.0;*/
            table.Rows.LeftIndent = 0;
            table.LeftPadding = 0;
            table.RightPadding = 0;


            //Number
            var column = table.AddColumn(Unit.FromInch(0/*0.25*/));
            column.Format.Alignment = ParagraphAlignment.Center;

            //Who

            column = table.AddColumn(Unit.FromInch(0.75));
            column.Format.Alignment = ParagraphAlignment.Center;

            //Measurable
            column = table.AddColumn(Unit.FromInch(2.0+.25));
            column.Format.Alignment = ParagraphAlignment.Center;

            //Goal
            column = table.AddColumn(Unit.FromInch(0.75));
            column.Format.Alignment = ParagraphAlignment.Center;

            //Measured
            for (var i = 0; i < 13; i++)
            {
                column = table.AddColumn(Unit.FromInch(6.25 / 13.0));
                column.Format.Alignment = ParagraphAlignment.Center;
            }

            //rows

            var row = table.AddRow();
            row.HeadingFormat = true;
            row.Format.Alignment = ParagraphAlignment.Center;
            row.Format.Font.Bold = true;
            row.Shading.Color = TableGray;
            row.Height = Unit.FromInch(0.25);

            row.Cells[1].AddParagraph("Who");
            row.Cells[1].VerticalAlignment = VerticalAlignment.Bottom;

            row.Cells[2].AddParagraph("Measurable");
            row.Cells[2].VerticalAlignment = VerticalAlignment.Bottom;

            row.Cells[3].AddParagraph("Goal");
            row.Cells[3].VerticalAlignment = VerticalAlignment.Bottom;

            var numWeeks = 13;

            var weeks = recur.Scorecard.Weeks.OrderByDescending(x => x.ForWeekNumber).Take(numWeeks).OrderBy(x => x.ForWeekNumber);
            var ii = 0;
            foreach (var w in weeks)
            {
                row.Cells[4 + ii].AddParagraph(w.DisplayDate.ToString("MM/dd/yy") + " to " + w.DisplayDate.AddDays(6).ToString("MM/dd/yy"));
                row.Cells[4 + ii].Format.Font.Size = Unit.FromInch(0.07);
                row.Cells[4 + ii].Format.Font.Size = Unit.FromInch(0.07);
                ii++;
            }
            //var r = new Random();

            var measurables = recur.Scorecard.Measurables.OrderBy(x => x.Ordering).Where(x => !(x.Disabled ?? false) && !x.IsDivider);
            var mn = 1;

            //for (var k = 0; k < 2; k++){
            foreach (var m in measurables)
            {

                row = table.AddRow();
                row.HeadingFormat = false;
                row.Format.Alignment = ParagraphAlignment.Center;

                row.Format.Font.Bold = false;
                row.Format.Font.Size = Unit.FromInch(0.128 * 2.0 / 3.0); // --- 1/16"
                //row.Shading.Color = TableBlue;
                row.HeightRule = RowHeightRule.AtLeast;
                row.VerticalAlignment = VerticalAlignment.Center;
                row.Height = Unit.FromInch((6 * 8 + 5.0) / (8 * 16.0) / 2);
                //row.Cells[0].AddParagraph("" + mn + ".");
                //row.Cells[0].Format.Alignment = ParagraphAlignment.Right;
                row.Cells[1].AddParagraph(m.Owner.Name);
                row.Cells[2].AddParagraph(m.Name);
                row.Cells[2].Format.Alignment = ParagraphAlignment.Left;

                var modifier = m.Modifiers ?? (RadialReview.Models.Enums.UnitType.None);

                row.Cells[3].AddParagraph((m.Direction ?? LessGreater.LessThan).ToSymbol() + " " + modifier.Format(m.Target ?? 0));
                ii = 0;
                foreach (var w in weeks)
                {
                    var found = recur.Scorecard.Scores.FirstOrDefault(x => x.ForWeek == w.ForWeekNumber && x.Measurable.Id == m.Id);
                    if (found != null && found.Measured.HasValue)
                    {
                        var val = found.Measured ?? 0;
                        row.Cells[4 + ii].AddParagraph(modifier.Format(val.KiloFormat()));
                    }
                    ii++;
                }
                mn += 1;
            }
            //}


        }


    }
}