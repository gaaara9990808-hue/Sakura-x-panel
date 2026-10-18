using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Uri = Android.Net.Uri;

namespace SakuraXPanel;

[Service(Name = "com.sakura.xpanel.ControlService", Label = "Sakura X functional overlay", Exported = false,
    ForegroundServiceType = ForegroundService.TypeSpecialUse)]
public sealed class ControlService : Service
{
    private const string Channel = "sakura_controls";
    private IWindowManager? wm;
    private DeviceControls? controls;
    private Button? bubble;
    private ScrollView? menu;
    private WindowManagerLayoutParams? bubbleParams;
    private WindowManagerLayoutParams? menuParams;
    private TextView? feedback;
    private TextView? telemetry;
    private bool awake;
    private bool isStopped;

    private static Color Pink => Color.Rgb(248, 70, 139);
    private static Color Bg => Color.Rgb(19, 16, 24);
    private static Color TextColor => Color.Rgb(246, 240, 250);
    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (wm != null) return StartCommandResult.NotSticky;
        try
        {
            var nm = (NotificationManager?)GetSystemService(NotificationService);
            nm?.CreateNotificationChannel(new NotificationChannel(Channel, "Sakura X controls", NotificationImportance.Low));
            var notification = new Notification.Builder(this, Channel)
                .SetSmallIcon(Android.Resource.Drawable.IcMenuManage)
                .SetContentTitle("Sakura X • controles ativos")
                .SetContentText("Toque na bolha para abrir; botão PARAR no painel")
                .SetOngoing(true).Build();
            StartForeground(2026, notification);
            if (!Settings.CanDrawOverlays(this)) throw new InvalidOperationException("Sobreposição não autorizada");
            wm = GetSystemService(WindowService)?.JavaCast<IWindowManager>();
            if (wm == null) throw new InvalidOperationException("Gerenciador de janelas indisponível");
            controls = new DeviceControls(this);
            awake = controls.KeepAwake;
            ShowBubble();
        }
        catch (Exception e)
        {
            Android.Util.Log.Error("SakuraX", "Falha ao iniciar: " + e);
            StopSelf();
        }
        return StartCommandResult.NotSticky;
    }
    private int Dp(int value) => (int)Math.Round(value * (Resources?.DisplayMetrics?.Density ?? 1f));
    private int ScreenWidth => Resources?.DisplayMetrics?.WidthPixels ?? Dp(360);
    private int ScreenHeight => Resources?.DisplayMetrics?.HeightPixels ?? Dp(740);
    private WindowManagerLayoutParams Params(int width, int height)
    {
        var p = new WindowManagerLayoutParams(width, height, WindowManagerTypes.ApplicationOverlay,
            WindowManagerFlags.NotFocusable | WindowManagerFlags.NotTouchModal, Format.Translucent);
        p.Gravity = GravityFlags.Left | GravityFlags.Top;
        if (awake) p.Flags |= WindowManagerFlags.KeepScreenOn;
        return p;
    }
    private GradientDrawable Background(Color color, int radius = 14)
    {
        var g = new GradientDrawable();
        g.SetColor(color);
        g.SetCornerRadius(Dp(radius));
        return g;
    }
    private Button Button(string text, Action click, bool primary = true)
    {
        var b = new Button(this) { Text = text, TextSize = 12 };
        b.SetAllCaps(false);
        b.SetTextColor(TextColor);
        b.Background = Background(primary ? Pink : Color.Rgb(56, 42, 65), 9);
        b.SetPadding(Dp(4), Dp(2), Dp(4), Dp(2));
        b.Click += (_, _) =>
        {
            try { click(); }
            catch (Exception e) { Feedback("Erro: " + e.Message); }
        };
        return b;
    }
    private TextView Label(string content, int size = 13, bool highlight = false)
    {
        var t = new TextView(this) { Text = content, TextSize = size };
        t.SetTextColor(highlight ? Pink : TextColor);
        t.SetPadding(Dp(4), Dp(6), Dp(4), Dp(6));
        return t;
    }
    private void AddButton(LinearLayout col, string content, Action callback, bool primary = false)
    {
        var b = Button(content, callback, primary);
        var lp = new LinearLayout.LayoutParams(-1, Dp(44));
        lp.SetMargins(Dp(3), Dp(3), Dp(3), Dp(3));
        col.AddView(b, lp);
    }
    private void ShowBubble()
    {
        if (wm == null || bubble != null || isStopped) return;
        bubble = Button("SX", OpenMenu);
        bubble.TextSize = 16;
        bubbleParams = Params(Dp(57), Dp(53));
        bubbleParams.X = Dp(8);
        bubbleParams.Y = Math.Max(Dp(65), ScreenHeight / 3);
        wm.AddView(bubble, bubbleParams);
        // Drag support: tap opens the menu; dragging moves the shortcut.
        float startX = 0, startY = 0;
        int originalX = 0, originalY = 0;
        bool dragged = false;
        bubble.Touch += (_, e) =>
        {
            var ev = e.Event;
            if (ev == null || bubbleParams == null || bubble == null) return;
            switch (ev.ActionMasked)
            {
                case MotionEventActions.Down:
                    startX = ev.RawX; startY = ev.RawY;
                    originalX = bubbleParams.X; originalY = bubbleParams.Y;
                    dragged = false; e.Handled = true;
                    break;
                case MotionEventActions.Move:
                    if (Math.Abs(ev.RawX - startX) > Dp(9) || Math.Abs(ev.RawY - startY) > Dp(9)) dragged = true;
                    if (dragged)
                    {
                        bubbleParams.X = Math.Clamp(originalX + (int)(ev.RawX - startX), 0, Math.Max(0, ScreenWidth - Dp(57)));
                        bubbleParams.Y = Math.Clamp(originalY + (int)(ev.RawY - startY), 0, Math.Max(0, ScreenHeight - Dp(53)));
                        try { wm?.UpdateViewLayout(bubble, bubbleParams); } catch { }
                    }
                    e.Handled = true;
                    break;
                case MotionEventActions.Up:
                    e.Handled = true;
                    if (!dragged) OpenMenu();
                    break;
                default: e.Handled = true; break;
            }
        };
    }
    private void RemoveBubble()
    {
        if (bubble == null) return;
        try { wm?.RemoveView(bubble); } catch { }
        bubble = null;
    }
    private void OpenMenu()
    {
        RemoveBubble();
        if (wm == null || menu != null || controls == null) return;
        var root = new LinearLayout(this) { Orientation = Orientation.Vertical };
        root.Background = Background(Bg);
        root.SetPadding(Dp(10), Dp(8), Dp(10), Dp(10));
        var title = Label("✿  SAKURA X  •  CONTROLES REAIS", 16, true);
        title.SetTypeface(null, TypefaceStyle.Bold);
        root.AddView(title);
        root.AddView(Label("Sem injeção • Sem scripts no jogo • Sem key", 10));
        AddButton(root, "◀ MINIMIZAR PAINEL", CloseMenu, true);
        root.AddView(Label("PERFIS", 13, true));
        AddButton(root, "JOGO • manter tela ligada e ajustar tela/áudio", () => ApplyProfile("Jogo"));
        AddButton(root, "ECONOMIA • brilho menor e tela livre", () => ApplyProfile("Economia"));
        root.AddView(Label("CONTROLES DO ANDROID", 13, true));
        var wake = new Switch(this) { Text = "Manter tela ligada (enquanto painel ativo)", Checked = awake, TextSize = 12 };
        wake.SetTextColor(TextColor);
        wake.CheckedChange += (_, e) => { UpdateAwake(e.IsChecked); Feedback(e.IsChecked ? "Tela mantida ligada." : "Tela pode desligar normalmente."); };
        root.AddView(wake);
        var brightnessLabel = Label("Brilho do sistema", 12);
        root.AddView(brightnessLabel);
        var brightness = new SeekBar(this) { Max = 255 - 15, Progress = Math.Clamp(controls.Brightness - 15, 0, 240) };
        // Only write on drag release, avoiding dozens of writes per second.
        brightness.StopTrackingTouch += (_, _) =>
        {
            if (controls.TrySetBrightness(brightness.Progress + 15, out var message))
                controls.SelectedProfile = "Personalizado";
            Feedback(message);
        };
        root.AddView(brightness);
        var volLabel = Label("Volume de mídia", 12);
        root.AddView(volLabel);
        var volume = new SeekBar(this) { Max = controls.MaxMediaVolume, Progress = controls.MediaVolume };
        volume.StopTrackingTouch += (_, _) =>
        {
            if (controls.TrySetMediaVolume(volume.Progress, out var message))
                controls.SelectedProfile = "Personalizado";
            Feedback(message);
        };
        root.AddView(volume);
        AddButton(root, "ATIVAR NÃO PERTURBE", () => TryDnd(true));
        AddButton(root, "DESATIVAR NÃO PERTURBE", () => TryDnd(false));
        root.AddView(Label("ESTADO DO APARELHO", 13, true));
        telemetry = Label("Lendo bateria e memória...", 12);
        root.AddView(telemetry);
        AddButton(root, "ATUALIZAR BATERIA / MEMÓRIA", RefreshTelemetry);
        root.AddView(Label("ATALHOS DE CONFIGURAÇÃO", 13, true));
        AddButton(root, "CONFIGURAÇÃO DE BRILHO (PERMISSÃO)", () => LaunchSettings(Settings.ActionManageWriteSettings, true));
        AddButton(root, "CONFIGURAÇÃO NÃO PERTURBE", () => LaunchSettings(Settings.ActionNotificationPolicyAccessSettings, false));
        AddButton(root, "CONFIGURAÇÃO DE BATERIA", () => LaunchSettings(Settings.ActionBatterySaverSettings, false));
        root.AddView(Label("Não existe 'FPS desbloqueado' nem anti-ban. Este app controla apenas funções autorizadas do Android.", 11));
        feedback = Label("Painel ativo. Perfil: " + controls.SelectedProfile, 11, true);
        root.AddView(feedback);
        AddButton(root, "PARAR PAINEL E SERVIÇO", () => StopSelf(), true);
        menu = new ScrollView(this) { FillViewport = false };
        menu.AddView(root);
        menuParams = Params(Math.Max(Dp(245), Math.Min(Dp(355), ScreenWidth - Dp(24))),
            Math.Min(Dp(545), Math.Max(Dp(250), ScreenHeight - Dp(30))));
        menuParams.X = Dp(12);
        menuParams.Y = Math.Max(0, Math.Min(Dp(35), ScreenHeight / 14));
        wm.AddView(menu, menuParams);
        RefreshTelemetry();
    }
    private void Feedback(string message)
    {
        if (feedback != null) feedback.Text = message;
        Android.Util.Log.Info("SakuraX", message);
    }
    private void ApplyProfile(string name)
    {
        if (controls == null) return;
        var message = controls.ApplyProfile(name);
        UpdateAwake(controls.KeepAwake);
        Feedback(message + ". Reabra painel para sincronizar sliders.");
    }
    private void UpdateAwake(bool state)
    {
        awake = state;
        if (controls != null) controls.KeepAwake = state;
        if (menuParams != null && menu != null)
        {
            menuParams.Flags = state ? menuParams.Flags | WindowManagerFlags.KeepScreenOn : menuParams.Flags & ~WindowManagerFlags.KeepScreenOn;
            try { wm?.UpdateViewLayout(menu, menuParams); } catch { }
        }
        if (bubbleParams != null && bubble != null)
        {
            bubbleParams.Flags = state ? bubbleParams.Flags | WindowManagerFlags.KeepScreenOn : bubbleParams.Flags & ~WindowManagerFlags.KeepScreenOn;
            try { wm?.UpdateViewLayout(bubble, bubbleParams); } catch { }
        }
    }
    private void TryDnd(bool on)
    {
        if (controls == null) return;
        controls.TrySetDnd(on, out var message);
        Feedback(message);
    }
    private void RefreshTelemetry()
    {
        if (controls == null || telemetry == null) return;
        var s = controls.ReadStatus();
        string battery = s.battery >= 0 ? $"{s.battery}%" : "não disponível";
        string temp = s.batteryTemp >= 0 ? $"{s.batteryTemp:0.0}°C (bateria)" : "não disponível";
        telemetry.Text = $"Bateria: {battery} • Temp.: {temp}\nRAM livre: {s.memAvailable} MB de {s.memTotal} MB\nMemória baixa: {(s.lowMem ? "SIM" : "não")}";
    }
    private void LaunchSettings(string action, bool withPackage)
    {
        try
        {
            var intent = new Intent(action);
            if (withPackage) intent.SetData(Uri.Parse("package:" + PackageName));
            intent.AddFlags(ActivityFlags.NewTask);
            StartActivity(intent);
            Feedback("Abra Sakura X novamente após conceder permissão.");
        }
        catch (Exception e) { Feedback("Não foi possível abrir configurações: " + e.Message); }
    }
    private void CloseMenu()
    {
        if (menu != null) { try { wm?.RemoveView(menu); } catch { } menu = null; }
        feedback = null;
        telemetry = null;
        menuParams = null;
        ShowBubble();
    }
    public override void OnDestroy()
    {
        isStopped = true;
        RemoveBubble();
        if (menu != null) { try { wm?.RemoveView(menu); } catch { } menu = null; }
        feedback = null;
        telemetry = null;
        wm = null;
        controls = null;
        base.OnDestroy();
    }
}
