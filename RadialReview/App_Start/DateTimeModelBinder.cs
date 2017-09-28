using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.App_Start {
    public class DateTimeModelBinder : IModelBinder {
        
            private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

       

        /// <summary>
        /// Fixes date parsing issue when using GET method. Modified from the answer given here:
        /// https://stackoverflow.com/questions/528545/mvc-datetime-binding-with-incorrect-date-format
        /// </summary>
        /// <param name="controllerContext">The controller context.</param>
        /// <param name="bindingContext">The binding context.</param>
        /// <returns>
        /// The converted bound value or null if the raw value is null or empty or cannot be parsed.
        /// </returns>
        public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext) {
                var vpr = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

                if (vpr == null) {
                    return null;

                }

                var date = vpr.AttemptedValue;

                if (String.IsNullOrEmpty(date)) {
                    return null;
                }

               // logger.DebugFormat("Parsing bound date '{0}' as US format.", date);

                // Set the ModelState to the first attempted value before we have converted the date. This is to ensure that the ModelState has
                // a value. When we have converted it, we will override it with a full universal date.
                bindingContext.ModelState.SetModelValue(bindingContext.ModelName, bindingContext.ValueProvider.GetValue(bindingContext.ModelName));

                try {
                    var realDate = DateTime.Parse(date, System.Globalization.CultureInfo.GetCultureInfoByIetfLanguageTag("en-us"));

                    // Now set the ModelState value to a full value so that it can always be parsed using InvarianCulture, which is the
                    // default for QueryStringValueProvider.
                    bindingContext.ModelState.SetModelValue(bindingContext.ModelName, new ValueProviderResult(date, realDate.ToString("yyyy-MM-dd hh:mm:ss"), System.Globalization.CultureInfo.GetCultureInfoByIetfLanguageTag("en-us")));

                    return realDate;
                } catch (Exception) {
                    logger.ErrorFormat("Error parsing bound date '{0}' as US format.", date);

                    bindingContext.ModelState.AddModelError(bindingContext.ModelName, String.Format("\"{0}\" is invalid.", bindingContext.ModelName));
                    return null;
                }
            }
        }
    }