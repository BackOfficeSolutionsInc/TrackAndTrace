using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Models.FirePad;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TractionTools.Tests.Crosscutting.Integrations {
	[TestClass]
	public class UnitTest1 {

		[TestMethod]
		public void TestMethod1() {
			var jsn= "{\"A0\":{\"a\":\"-LPtVbk7sIq3ErXy2Utz\",\"o\":[{\"l\":true},\"\",\"testing my codes\"],\"t\":1540710631778},\"A1\":{\"a\":\"-LPtVbk7sIq3ErXy2Utz\",\"o\":[12,\"\n\",5],\"t\":1540710699008},\"A2\":{\"a\":\"-LPtVbk7sIq3ErXy2Utz\",\"o\":[13,{\"l\":true},\"\",5],\"t\":1540710699248},\"A3\":{\"a\":\"-LPuWe3yvuLlMynUnK8F\",\"o\":[{\"lt\":\"u\"},1,18],\"t\":1540727687379},\"A4\":{\"a\":\"-LPuWe3yvuLlMynUnK8F\",\"o\":[13,{\"lt\":\"u\"},1,5],\"t\":1540727688031},\"A5\":{\"a\":\"-LQ6DGkjWox8qD8lYevq\",\"o\":[19,\"\n\"],\"t\":1540940708305},\"A6\":{\"a\":\"-LQ6DGkjWox8qD8lYevq\",\"o\":[20,{\"l\":true,\"lt\":\"u\"},\"\"],\"t\":1540940708601},\"A7\":{\"a\":\"-LQ6DGkjWox8qD8lYevq\",\"o\":[21,\"t\"],\"t\":1540940710251},\"A8\":{\"a\":\"-LQ6DGkjWox8qD8lYevq\",\"o\":[22,\"e\"],\"t\":1540940710603},\"A9\":{\"a\":\"-LQ6DGkjWox8qD8lYevq\",\"o\":[23,\"s\"],\"t\":1540940710932},\"AA\":{\"a\":\"-LQ6DGkjWox8qD8lYevq\",\"o\":[24,\"t\"],\"t\":1540940711284},\"AB\":{\"a\":\"-LQ6DGkjWox8qD8lYevq\",\"o\":[25,\"i\"],\"t\":1540940712331},\"AC\":{\"a\":\"-LQ6DGkjWox8qD8lYevq\",\"o\":[26,\"b\"],\"t\":1540940712822},\"AD\":{\"a\":\"-LQ6DGkjWox8qD8lYevq\",\"o\":[26,-1],\"t\":1540940713651},\"AE\":{\"a\":\"-LQ6DGkjWox8qD8lYevq\",\"o\":[26,\"n\"],\"t\":1540940714301},\"AF\":{\"a\":\"-LQ6DGkjWox8qD8lYevq\",\"o\":[27,\"g\"],\"t\":1540940714681},\"AG\":{\"a\":\"-LQCMeD7WQuP1DDbnrIq\",\"o\":[13,{\"lt\":\"o\"},1,14],\"t\":1541043836917},\"AH\":{\"a\":\"-LQCMeD7WQuP1DDbnrIq\",\"o\":[20,{\"lt\":\"o\"},1,7],\"t\":1541043837148},\"AI\":{\"a\":\"-LQgmdLH0E6-oCH6Dms7\",\"o\":[21,{\"b\":true},7],\"t\":1541571406738},\"AJ\":{\"a\":\"-LQguW3RiQ0Q4Qaahh9C\",\"o\":[14,{\"i\":true},5,9],\"t\":1541575010996},\"AK\":{\"a\":\"-LQzLALB0DGwcwUU1mHo\",\"o\":[28,{\"b\":true},\"m\"],\"t\":1541882305892},\"AL\":{\"a\":\"-LQzLALB0DGwcwUU1mHo\",\"o\":[29,{\"b\":true},\"\n\"],\"t\":1541882308798},\"AM\":{\"a\":\"-LQzLALB0DGwcwUU1mHo\",\"o\":[30,{\"l\":true,\"lt\":\"o\"},\"\",{\"b\":true},\"6\"],\"t\":1541882309036},\"AN\":{\"a\":\"-LQzLALB0DGwcwUU1mHo\",\"o\":[32,{\"b\":true},\"7\"],\"t\":1541882309276},\"AO\":{\"a\":\"-LQzLALB0DGwcwUU1mHo\",\"o\":[33,{\"b\":true},\"7\"],\"t\":1541882309524},\"AP\":{\"a\":\"-LQzLNUPUi7-MEMqfVky\",\"o\":[34,{\"b\":true},\"8\"],\"t\":1541882356541},\"AQ\":{\"a\":\"-LQzLeuQ1EUxnuTFr9vn\",\"o\":[35,{\"b\":true},\"o\"],\"t\":1541882456772},\"AR\":{\"a\":\"-LQzLeuQ1EUxnuTFr9vn\",\"o\":[12,\"m\",24],\"t\":1541882504371},\"AS\":{\"a\":\"-LQzNwVGcljOxYdc-G5G\",\"o\":[37,{\"b\":true},\" \"],\"t\":1541883092623}}";
			Dictionary<string, Dictionary<string, object>> items = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(jsn);
			var fd = new FirePadData();
			fd.setHtml(items);
		}
	}
}
