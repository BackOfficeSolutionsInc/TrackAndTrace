using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RadialReview.Tests.Samples.Optimization.Framework;

namespace RadialReview.Tests
{
    class ProblemRunner
    {
        static void Main(string[] args)
        {
			TransportproblemSample.Run();
			WarehouseproblemSample.Run();
			ProductionmixSample.Run();
			SteelProductionSample.Run();
			MultiTransportSample.Run();
			multmip1.Run();
			multmip2.Run();
			multmip3.Run();
			steel3.Run();
			steel4.Run();
			TransportSample.Run();
        }
    }
}
