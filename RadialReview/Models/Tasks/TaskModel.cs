using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using System;

namespace RadialReview.Models.Tasks
{
    public class TaskModel : ILongIdentifiable
    {
        public virtual long Id { get; set; }
        public virtual String Name { get; set; }
        public virtual DateTime DueDate { get; set; }
        public virtual ICompletionModel Completion { get; set; }
        public virtual TaskType Type { get; set; }
        public virtual int Count { get; set; }
    }
}