using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ApiDesign.Models.Database;
using ApiDesign.Models.DTO.V1;
using ApiDesign.Models.Interfaces;
using ApiDesign.Models.DTO.V2;
using ApiDesign.Accessors;
using System.Linq;
using ApiDesign.Utilites.DTO;

namespace ApiDesign.Tests.Dto {
	[TestClass]
	public class DtoConverterTest {
		[TestMethod]
		public void TestGet() {

			DtoConverter.RegisterConverter(new RockDtoV1_Converter());
			DtoConverter.RegisterConverter(new RockDtoV2());
			DtoConverter.RegisterConverter(new UserOrgDtoV2.Converter());

			var id = 1;
			var name = "ROCK";
			var owner = 100;
			var rock = new RockModel() {
						Id = id,
						Rock = name,
						OwnerId = owner
			};
			//Test V1
			{
				var v1_model = DtoConverter.Convert(Const.API.V1, rock);
				Assert.IsTrue(v1_model is RockDtoV1);
				var v1_model_resolved = v1_model as RockDtoV1;
				Assert.AreEqual(id, v1_model_resolved.Id);
				Assert.AreEqual(name, v1_model_resolved.MyName);
				Assert.AreEqual(true, v1_model_resolved.HasOwner);

			}
			//Test V2
			{
				var v2_model = DtoConverter.Convert(Const.API.V2, rock);
				Assert.IsTrue(v2_model is RockDtoV2);
				var v2_model_resolved = v2_model as RockDtoV2;
				Assert.AreEqual(name, v2_model_resolved.Name);
				Assert.AreEqual(owner, v2_model_resolved.Owner.Id);
			}
		}
	}
}
