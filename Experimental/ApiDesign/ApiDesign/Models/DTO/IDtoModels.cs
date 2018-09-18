using ApiDesign.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiDesign.Models.DTO {

	public interface IDto {
		long Id { get; }
	}

	public interface IDto<T> : IDto{
	}

	public interface IDtoFactory {
		IEnumerable<int> ForVersions();
	}
	
	public interface IDtoFactory<T> : IDtoFactory where T : IBackend<T> {
		/*Caution refactoring. Method name is reflected in DtoConverter.*/
		IDto<T> Convert(T model, IDtoFactoryHelper helper);

	}

	public interface IDtoFactoryHelper {
		IDto<T> Convert<T>(IBackend<T> submodel) where T : IBackend<T>;
	}
}
