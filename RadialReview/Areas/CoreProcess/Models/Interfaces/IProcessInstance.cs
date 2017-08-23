using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Areas.CoreProcess.Models.Interfaces
{
    public interface IProcessInstance
    {
        string GetId();

        string GetDefinitionId();

        string GetBusinessKey();

        string GetCaseInstanceId();

        bool GetEnded();

        bool GetSuspended();

    }
}
