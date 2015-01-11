using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Models.Interfaces
{
    public interface ICompletionModel
    {
        bool Started { get; }
        bool FullyComplete { get; }
        List<CompletionModel> GetCompletions();
	    decimal GetPercentage();
        bool Illegal { get; }
    }
}
