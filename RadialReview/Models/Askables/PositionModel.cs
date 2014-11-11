using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models.Askables
{
    public class PositionModel :ILongIdentifiable
    {
        public virtual long Id { get;protected set; }
        public virtual LocalizedStringModel Name { get; set; }
    }

    public class PositionModelMap : ClassMap<PositionModel>
    {
        public PositionModelMap()
        {
            Id(x => x.Id);
            References(x => x.Name)
                .Not.LazyLoad()
                .Cascade.SaveUpdate();
        }
    }
  
}