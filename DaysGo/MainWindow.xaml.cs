namespace DaysGo;

using System.Windows;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media;

public partial class MainWindow : Window
{
    private NotifyIcon? _notifyIcon;
    
    // 常量
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TRANSPARENT = 0x00000020; // 关键：允许鼠标点击穿透窗口

    private IntPtr _hwnd;
    private long _initialStyle;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        
        // 订阅 Loaded 事件，确保窗口大小已确定
        Loaded += MainWindow_Loaded;
        
        // 关键：订阅每帧渲染事件，用于实时检测键盘状态（比全局钩子更轻量、安全）
        CompositionTarget.Rendering += OnRendering;
        
        SetupTrayIcon();
    }
    
    private void SetupTrayIcon()
    {
        _notifyIcon = new NotifyIcon();
        
        // 加载图标：从资源中读取
        var iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/app.ico"))?.Stream;
        if (iconStream != null)
        {
            _notifyIcon.Icon = new Icon(iconStream);
        }

        _notifyIcon.Visible = true;
        _notifyIcon.Text = "桌面倒数日"; // 鼠标悬停文字

        // 实现你的需求：左键点击图标直接退出程序
        _notifyIcon.MouseClick += (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                ExitApp();
            }
        };
        
        // 建议：右键可以加个简单的菜单（可选）
        var menu = new ContextMenuStrip();
        menu.Items.Add("退出 (Exit)", null, (s, e) => ExitApp());
        _notifyIcon.ContextMenuStrip = menu;
    }

    private void ExitApp()
    {
        _notifyIcon?.Dispose(); // 必须释放，否则图标会残留在托盘区域
        Application.Current.Shutdown();
    }
    
    // 确保窗口关闭时图标也被释放
    protected override void OnClosed(EventArgs e)
    {
        _notifyIcon?.Dispose();
        base.OnClosed(e);
    }


    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwnd = new WindowInteropHelper(this).Handle;
        
        // 获取初始样式并强制开启：不激活 + 完全穿透
        _initialStyle = GetWindowLongPtr(_hwnd, GWL_EXSTYLE).ToInt64();
        ApplyWindowStyle(true); 
    }
    
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // 1. 获取主屏幕的工作区域（排除掉任务栏占用的空间）
        var workingArea = SystemParameters.WorkArea;

        // 2. 获取当前窗口的尺寸
        // 注意：如果你的窗口是 SizeToContent，此时高度已经由内容撑开
        double windowWidth = this.ActualWidth;
        double windowHeight = this.ActualHeight;

        // 3. 计算目标位置
        // 右边距 8%：(屏幕宽度 - 窗口宽度) - (屏幕宽度 * 0.08)
        double targetLeft = workingArea.Right - windowWidth - (workingArea.Width * 0.08);
    
        // 底边距 20%：(屏幕高度 - 窗口高度) - (屏幕高度 * 0.20)
        double targetTop = workingArea.Bottom - windowHeight - (workingArea.Height * 0.20);

        // 4. 应用坐标
        this.Left = targetLeft;
        this.Top = targetTop;
    }
    
    
    private void OnRendering(object? sender, EventArgs e)
    {
        // 实时检测 Shift 键状态 (2026年 .NET 10 依然有效的 Win32 方案)
        bool isShiftDown = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

        // 按下 Shift 时：关闭穿透，允许交互
        ApplyWindowStyle(!isShiftDown);
        // 没按 Shift 时：开启穿透，像空气一样
    }
    
    private void ApplyWindowStyle(bool transparent)
    {
        long targetStyle = _initialStyle | WS_EX_NOACTIVATE;
        if (transparent)
        {
            targetStyle |= WS_EX_TRANSPARENT;
        }

        // 仅在样式发生变化时更新，避免闪烁或性能损耗
        long currentStyle = GetWindowLongPtr(_hwnd, GWL_EXSTYLE).ToInt64();
        if (currentStyle != targetStyle)
        {
            SetWindowLongPtr(_hwnd, GWL_EXSTYLE, new IntPtr(targetStyle));
        }
    }


    // 响应拖动：此时由于 Shift 已按下，穿透已关闭，此事件可以正常触发
    private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            this.DragMove();
        }
    }

    // 兼容 32/64 的 Get/SetWindowLongPtr 封装
    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static partial IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static partial IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);

    private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
    {
        if (IntPtr.Size == 8)
            return GetWindowLongPtr64(hWnd, nIndex);
        else
            return GetWindowLong32(hWnd, nIndex);
    }

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static partial IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static partial IntPtr SetWindowLong32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        if (IntPtr.Size == 8)
            return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
        else
            return SetWindowLong32(hWnd, nIndex, dwNewLong);
    }

    [LibraryImport("user32.dll")]
    private static partial IntPtr SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, int uFlags);
}