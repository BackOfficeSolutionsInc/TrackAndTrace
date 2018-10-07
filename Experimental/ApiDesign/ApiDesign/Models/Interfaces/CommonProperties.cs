using System;

namespace ApiDesign.Models.Interfaces {
	public interface ILongIdentifiableModel {
		long GetId();
	}

	public interface INameableModel {
		string GetName();
	}

	public interface IHistoricalModel : ICreateableModel, IDeletableModel {
	}
	public interface ICreateableModel {
		DateTime GetCreateTime();
	}
	public interface IDeletableModel {
		DateTime? GetDeleteTime();
	}

	public interface IBackend : ILongIdentifiableModel {
	}

	public interface IBackend<T> : IBackend where T : IBackend<T> {
	}	
}
