using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using RadialReview.Models.Interfaces;

namespace RadialReview
{
    public static class PaginationExtensions
    {
        public static int PageCount<T>(this IEnumerable<T> self, int resultPerPage)
        {
            return (int)Math.Ceiling(self.Count() / ((double)resultPerPage));
        }
        public static IEnumerable<T> Paginate<T>(this IEnumerable<T> self, int page, int resultPerPage)
        {
            return self.Skip(page * resultPerPage).Take(resultPerPage);
        }

        /*public static U SetPagination<T,U>(this U self,Func<U, IEnumerable<T>> itemSelector, int page, int resultPerPage) where U : IPagination
        {
            var selected = itemSelector(self);
            self.NumPages = selected.;
            self.Page = page;
            
            return self;
        }*/
    }
}