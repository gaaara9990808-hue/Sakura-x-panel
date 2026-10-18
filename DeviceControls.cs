using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Provider;

namespace SakuraXPanel;

/// <summary>Android system actions only. No Free Fire memory access, hooking or input injection.</summary>
public sealed class DeviceControls
{
    private readonly Context ctx;
    private readonly ISharedPreferences prefs;
    public DeviceControls(Context context)
    {
        ctx = context;
        prefs = ctx.GetSharedPreferences("sakura_x_profiles", FileCreationMode.Private)!;
    }
    public bool KeepAwake
    {
        get => prefs.GetBoolean("awake", false);
        set => prefs.Edit()!.PutBoolean("awake", value)!.Apply();
    }
    public string SelectedProfile
    {
        get => prefs.GetString("profile", "Personalizado") ?? "Personalizado";
        set => prefs.Edit()!.PutString("profile", value)!.Apply();
    }
    public bool CanChangeBrightness => Settings.System.CanWrite(ctx);
    public int Brightness => Settings.System.GetInt(ctx.ContentResolver!, Settings.System.ScreenBrightness, 125);
    public int MediaVolume
    {
        get
        {
            var a = (AudioManager?)ctx.GetSystemService(Context.AudioService);
            return a?.GetStreamVolume(Android.Media.Stream.Music) ?? 0;
        }
    }
    public int MaxMediaVolume
    {
        get
        {
            var a = (AudioManager?)ctx.GetSystemService(Context.AudioService);
            return Math.Max(1, a?.GetStreamMaxVolume(Android.Media.Stream.Music) ?? 15);
        }
    }
    public bool TrySetBrightness(int level, out string message)
    {
        if (!CanChangeBrightness) { message = "Permita 'Modificar configurações do sistema'."; return false; }
        try
        {
            var value = Math.Clamp(level, 15, 255);
            // Brightness manual is intentional; user can restore automatic brightness later.
            Settings.System.PutInt(ctx.ContentResolver!, "screen_brightness_mode", 0);
            var saved = Settings.System.PutInt(ctx.ContentResolver!, Settings.System.ScreenBrightness, value);
            message = saved ? $"Brilho aplicado: {value}/255" : "Android recusou o ajuste de brilho.";
            return saved;
        }
        catch (Exception ex) { message = "Falha no brilho: " + ex.Message; return false; }
    }
    public bool TrySetMediaVolume(int value, out string message)
    {
        try
        {
            var a = (AudioManager?)ctx.GetSystemService(Context.AudioService);
            if (a == null) { message = "Áudio indisponível."; return false; }
            value = Math.Clamp(value, 0, a.GetStreamMaxVolume(Android.Media.Stream.Music));
            a.SetStreamVolume(Android.Media.Stream.Music, value, VolumeNotificationFlags.ShowUi);
            message = $"Volume de mídia ajustado: {value}/{MaxMediaVolume}";
            return true;
        }
        catch (Exception ex) { message = "Falha no volume: " + ex.Message; return false; }
    }
    public bool CanChangeDnd
    {
        get
        {
            var manager = (NotificationManager?)ctx.GetSystemService(Context.NotificationService);
            return manager?.IsNotificationPolicyAccessGranted ?? false;
        }
    }
    public bool TrySetDnd(bool enabled, out string message)
    {
        if (!CanChangeDnd) { message = "Autorize 'Não Perturbe' no aplicativo Sakura X."; return false; }
        try
        {
            var manager = (NotificationManager?)ctx.GetSystemService(Context.NotificationService);
            if (manager == null) { message = "Serviço Não Perturbe indisponível."; return false; }
            manager.SetInterruptionFilter(enabled ? InterruptionFilter.None : InterruptionFilter.All);
            message = enabled ? "Não Perturbe ativado." : "Não Perturbe desativado.";
            return true;
        }
        catch (Exception ex) { message = "Android recusou Não Perturbe: " + ex.Message; return false; }
    }
    public string ApplyProfile(string preset)
    {
        SelectedProfile = preset;
        if (preset == "Jogo")
        {
            KeepAwake = true;
            TrySetBrightness(165, out var b);
            TrySetMediaVolume((int)Math.Round(MaxMediaVolume * .7), out var v);
            return $"Perfil Jogo: tela ligada, {b}; {v}";
        }
        if (preset == "Economia")
        {
            KeepAwake = false;
            TrySetBrightness(80, out var b);
            TrySetMediaVolume((int)Math.Round(MaxMediaVolume * .35), out var v);
            return $"Perfil Economia: {b}; {v}";
        }
        return "Perfil Personalizado selecionado.";
    }
    public (int battery, float batteryTemp, int memAvailable, int memTotal, bool lowMem) ReadStatus()
    {
        int battery = -1;
        float temperature = -1;
        try
        {
            using var batteryIntent = ctx.RegisterReceiver(null, new IntentFilter(Intent.ActionBatteryChanged));
            if (batteryIntent != null)
            {
                int level = batteryIntent.GetIntExtra(BatteryManager.ExtraLevel, -1);
                int scale = batteryIntent.GetIntExtra(BatteryManager.ExtraScale, -1);
                if (level >= 0 && scale > 0) battery = (int)Math.Round(level * 100.0 / scale);
                int t = batteryIntent.GetIntExtra("temperature", -1);
                if (t >= 0) temperature = t / 10f; // battery temperature, not CPU
            }
        }
        catch { }
        var info = new ActivityManager.MemoryInfo();
        try { ((ActivityManager?)ctx.GetSystemService(Context.ActivityService))?.GetMemoryInfo(info); }
        catch { }
        return (battery, temperature, (int)(info.AvailMem / 1048576), (int)(info.TotalMem / 1048576), info.LowMemory);
    }
}
