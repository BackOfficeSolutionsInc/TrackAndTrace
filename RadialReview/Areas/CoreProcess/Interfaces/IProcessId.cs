﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Areas.CoreProcess.Interfaces
{
    public interface IProcessId
    {
        string LocalId { get; set; }
        string CamundaId { get; set; }
    }
}
