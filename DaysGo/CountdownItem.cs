namespace DaysGo;

using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

public partial class CountdownItem : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    [property: JsonConverter(typeof(DateOnlyConverter))] // 应用转换器
    private DateTime _targetDate;

    // 关键：天数是根据当前时间动态计算的
    // 使用 [ObservableProperty] 的关联通知，或者直接在界面绑定时计算
    [JsonIgnore]
    public int DaysRemaining => (TargetDate.Date - DateTime.Now.Date).Days;
    
    // 2. 提供一个手动触发界面刷新的方法
    public void RefreshDays() => OnPropertyChanged(nameof(DaysRemaining));
    
    public static DateTime GetEndOfYear() 
    {
        // 2026年运行，则返回 2026-12-31
        return new DateTime(DateTime.Now.Year, 12, 31);
    }
    
    // 在 CountdownItem.cs 中添加
    [JsonIgnore]
    public string UnitDisplay
    {
        get
        {
            bool isChinese = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.StartsWith("zh");
            if (isChinese) return "天";
            return DaysRemaining == 1 ? "DAY" : "DAYS";
        }
    }
}
