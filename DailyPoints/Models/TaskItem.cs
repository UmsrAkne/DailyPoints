using System;

namespace DailyPoints.Models
{
    public class TaskItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string IssueId { get; set; } = string.Empty;

        public string Summary { get; set; } = string.Empty;

        public TimeSpan Estimation { get; set; } // TimeSpan型

        public TimeSpan ActualTime { get; set; } // TimeSpan型

        public int Rate { get; set; } = 100;
    }
}