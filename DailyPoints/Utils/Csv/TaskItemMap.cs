using CsvHelper.Configuration;
using DailyPoints.Models;

namespace DailyPoints.Utils.Csv
{
    public sealed class TaskItemMap : ClassMap<TaskItem>
    {
        public TaskItemMap()
        {
            Map(m => m.IssueId).Name("Issue Id");
            Map(m => m.Summary).Name("Summary");

            // 自作した TimeSpan コンバーターを適用
            Map(m => m.Estimation).Name("Estimation").TypeConverter<MinuteToTimeSpanConverter>();
            Map(m => m.ActualTime).Name("経過時間").TypeConverter<MinuteToTimeSpanConverter>();

            Map(m => m.Rate).Name("Rate").Convert(args =>
            {
                var value = args.Row.GetField("Rate");
                if (value == null)
                {
                    return 100;
                }

                return value == "empty" ? 100 : int.Parse(value);
            });
        }
    }
}