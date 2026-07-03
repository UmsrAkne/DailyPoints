using System;
using System.Text.Json.Serialization;

namespace DailyPoints.Models
{
    public class TaskItem
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [JsonPropertyName("issue_id")]
        public string IssueId { get; set; } = string.Empty;

        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        [JsonIgnore]
        public TimeSpan Estimation { get; set; } // TimeSpan型

        [JsonPropertyName("estimation")]
        public int EstimationMinutes
        {
            get => (int)Estimation.TotalMinutes;
            set => Estimation = TimeSpan.FromMinutes(value);
        }

        [JsonIgnore]
        public TimeSpan ActualTime { get; set; } // TimeSpan型

        [JsonPropertyName("actual_time")]
        public int ActualTimeMinutes
        {
            get => (int)ActualTime.TotalMinutes;
            set => ActualTime = TimeSpan.FromMinutes(value);
        }

        [JsonPropertyName("rate")]
        public int Rate { get; set; } = 100;
    }
}