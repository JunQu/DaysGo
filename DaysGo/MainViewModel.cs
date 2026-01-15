namespace DaysGo;

using System.Globalization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Encodings.Web;
using System.Text.Unicode; 

public class MainViewModel : ObservableObject
{
    private readonly string _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data.json");
    private readonly DispatcherTimer _timer;
    
    // 判断是否为中文环境
    public bool IsChinese => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.StartsWith("zh");

    public ObservableCollection<CountdownItem> Items { get; } = new();

    public MainViewModel()
    {
        LoadData();

        // 每分钟刷新一次，确保跨越午夜时天数自动更新
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(30) };
        _timer.Tick += (s, e) => {
            foreach (var item in Items) item.RefreshDays();
        };
        _timer.Start();
    }
    
    private void LoadData()
    {
        
        if (!File.Exists(_filePath))
        {
            
            var endOfYear = CountdownItem.GetEndOfYear();
            var defaultItem = new CountdownItem 
            { 
                // 根据语言生成初始标题
                Title = IsChinese
                    ? $"{DateTime.Now.Year}年结束还有" 
                    : $"{DateTime.Now.Year} Year End", 
                TargetDate = endOfYear 
            };
        
            Items.Add(defaultItem);
        
            // 第一次运行时立即生成 data.json
            SaveData();
            return;
        }

        try
        {
            string json = File.ReadAllText(_filePath);
            var list = JsonSerializer.Deserialize<List<CountdownItem>>(json);
            if (list != null)
            {
                foreach (var item in list) Items.Add(item);
            }
        }
        catch (Exception ex) 
        {
            // 如果文件损坏，也可以选择加载默认值
            Console.WriteLine(ex.Message);
        }
    }
    
    // 保存数据：只需一行
    private void SaveData()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true, // 让 JSON 换行，方便人类阅读
            // 核心设置 1：支持中文不转义
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        };
        
        string jsonString = JsonSerializer.Serialize(Items, options);
        File.WriteAllText(_filePath, jsonString);
    }
}
