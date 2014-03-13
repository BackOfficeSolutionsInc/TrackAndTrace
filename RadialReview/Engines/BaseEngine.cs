using RadialReview.Accessors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Engines
{
    public class BaseEngine
    {
        protected static TeamAccessor _TeamAccessor = new TeamAccessor();
        protected static UserAccessor _UserAccessor = new UserAccessor();
        protected static NexusAccessor _NexusAccessor = new NexusAccessor();
        protected static ImageAccessor _ImageAccessor = new ImageAccessor();
        protected static GroupAccessor _GroupAccessor = new GroupAccessor();
        protected static OriginAccessor _OriginAccessor = new OriginAccessor();
        protected static ReviewAccessor _ReviewAccessor = new ReviewAccessor();
        protected static PaymentAccessor _PaymentAccessor = new PaymentAccessor();
        protected static KeyValueAccessor _KeyValueAccessor = new KeyValueAccessor();
        protected static PositionAccessor _PositionAccessor = new PositionAccessor();
        protected static QuestionAccessor _QuestionAccessor = new QuestionAccessor();
        protected static CategoryAccessor _CategoryAccessor = new CategoryAccessor();
        protected static PrereviewAccessor _PrereviewAccessor = new PrereviewAccessor();
        protected static PermissionsAccessor _PermissionsAccessor = new PermissionsAccessor();
        protected static OrganizationAccessor _OrganizationAccessor = new OrganizationAccessor();
        protected static ResponsibilitiesAccessor _ResponsibilitiesAccessor = new ResponsibilitiesAccessor();
    }
}