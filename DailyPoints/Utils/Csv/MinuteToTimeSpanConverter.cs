using System;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace DailyPoints.Utils.Csv
{
    public class MinuteToTimeSpanConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return TimeSpan.Zero;
            }

            // 正規表現で数値部分だけを抽出 ("20m" や "20" -> "20")
            var match = Regex.Match(text, @"\d+");
            if (match.Success && double.TryParse(match.Value, out var minutes))
            {
                // 抽出した数値を「分」として TimeSpan を生成
                return TimeSpan.FromMinutes(minutes);
            }

            return TimeSpan.Zero; // パース失敗時は 0 時間
        }
    }
}