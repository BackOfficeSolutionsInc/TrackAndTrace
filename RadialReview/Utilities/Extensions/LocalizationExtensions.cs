using log4net;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web;

namespace RadialReview
{
    public static class LocalizationExtensions
    {
        private static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        public static String Translate(this LocalizedStringModel model,String cultureCode=null)
        {
            //CultureInfo.GetCultures()
            var culture=Thread.CurrentThread.CurrentCulture.CultureTypes;
            if (model == null)
            {
                log.Error("LocalizedStringModel is null in translate");
                return null;
            }
            if (model.Def == null)
            {
                log.Error("Default is null for LocalizedStringModel (" + model.Id + ")");
                return "�";
            }
            if (model.Def.Value == null)
            {
                log.Error("Value is null for LocalizedStringModel (" + model.Id + ")");
                return "�";
            }

            return model.Def.Value;
        }

        public static void Update(this LocalizedStringModel model, LocalizedStringModel update)
        {
            foreach(var item in update.Localizations)
            {
                model.Update(item.Locale, item.Value);
            }
        }

        public static void Update(this LocalizedStringModel model,String cultureId, String value)
        {
            var found = model.Localizations.FirstOrDefault(x => x.Locale == cultureId);

            if (found == null)
                model.Localizations.Add(new LocalizedStringPairModel() {Locale=cultureId, Value=value});
            else
            {
                found.Value = value;
            }
        }

        public static void UpdateDefault(this LocalizedStringModel model,String value)
        {
            if (model.Def.Locale == null)
            {
                model.Def.Locale = Thread.CurrentThread.CurrentCulture.Name;
                model.Localizations.Add(new LocalizedStringPairModel() { Locale = model.Def.Locale, Value = value });
            }
            else
            {
                model.Localizations.FirstOrDefault(x => x.Locale == model.Def.Locale).NotNull(x => x.Value = value);
            }
            model.Def.Value = value;
        }


    }

}