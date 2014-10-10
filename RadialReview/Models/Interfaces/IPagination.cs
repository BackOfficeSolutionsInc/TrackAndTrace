using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Models.Interfaces
{
    public interface IPagination
    {
        double NumPages { get; set; }
        int Page { get; set; }

    }
}
