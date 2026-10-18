using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Views;
using Android.Widget;
using Uri = Android.Net.Uri;

namespace SakuraXPanel;

[Activity(Label = "Sakura X Panel", MainLauncher = true, Exported = true)]
public sealed class MainActivity : Activity
{
    private TextView? status;
    private static Color Pink => Color.Rgb(252, 67, 132);
    private static Color White => Color.Rgb(247, 241, 248);

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Window?.SetStatusBarColor(Color.Rgb(24, 13, 26));
        var root = new LinearLayout(this) { Orientation = Orientation.Vertical };
        root.SetPadding(28, 40, 28, 28);
        root.SetBackgroundColor(Color.Rgb(20, 15, 23));
        AddText(root, "SAKURA X", 30, Pink);
        AddText(root, "PAINEL FUNCIONAL ANDROID • V1.0", 14, White);
        AddText(root, "Ajusta funções reais do Android. Não injeta código, não altera dano, FPS ou mira do Free Fire.", 14, White);
        AddButton(root, "1  PERMITIR PAINEL FLUTUANTE", () => OpenSettings(Settings.ActionManageOverlayPermission, true));
        AddButton(root, "2  PERMITIR AJUSTES DE BRILHO", () => OpenSettings(Settings.ActionManageWriteSettings, true));
        AddButton(root, "3  PERMITIR NÃO PERTURBE", () => OpenSettings(Settings.ActionNotificationPolicyAccessSettings, false));
        AddButton(root, "4  ABRIR PAINEL FUNCIONAL", StartPanel);
        AddButton(root, "FECHAR PAINEL", () => { StopService(new Intent(this, typeof(ControlService))); ShowStatus("Painel encerrado."); });
        status = AddText(root, "Permissões opcionais para brilho e Não Perturbe. Sobreposição é obrigatória.", 12, Color.Rgb(175, 218, 250));
        var scroller = new ScrollView(this) { FillViewport = true };
        scroller.AddView(root);
        SetContentView(scroller);
    }

    private void OpenSettings(string action, bool withPackage)
    {
        try
        {
            var i = new Intent(action);
            if (withPackage) i.SetData(Uri.Parse("package:" + PackageName));
            StartActivity(i);
        }
        catch (Exception e) { ShowStatus("Configuração indisponível: " + e.Message); }
    }

    private void StartPanel()
    {
        if (!Settings.CanDrawOverlays(this))
        {
            ShowStatus("Autorize a sobreposição no passo 1.");
            return;
        }
        try
        {
            var intent = new Intent(this, typeof(ControlService));
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O) StartForegroundService(intent);
            else StartService(intent);
            ShowStatus("Painel iniciado. A bolha Sakura aparecerá na tela.");
        }
        catch (Exception e) { ShowStatus("Falha ao abrir painel: " + e.Message); }
    }

    private void ShowStatus(string text) { if (status != null) status.Text = text; }
    private TextView AddText(LinearLayout root, string text, int size, Color color)
    {
        var t = new TextView(this) { Text = text, TextSize = size };
        t.SetTextColor(color);
        t.SetPadding(3, 12, 3, 14);
        root.AddView(t);
        return t;
    }
    private void AddButton(LinearLayout root, string text, Action action)
    {
        var b = new Button(this) { Text = text, TextSize = 13 };
        b.SetTextColor(White);
        b.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Pink);
        root.AddView(b);
        b.Click += (_, _) => action();
    }
}
