using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Models.Interfaces {
    public interface IForModel {
        long ModelId { get; }
        string ModelType { get; }
        bool Is<T>();
    }
    public interface IByAbout {
        IForModel GetBy();
        IForModel GetAbout();
    }
}
