using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using InputStitch;

internal static class CornerCutDialogTests
{
    private const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance;
    private const BindingFlags StaticHidden = BindingFlags.NonPublic | BindingFlags.Static;
    private static int checks;
    private static T Field<T>(object value, string name) { return (T)value.GetType().GetField(name, Hidden).GetValue(value); }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
    private static void Pump() { Stopwatch timer = Stopwatch.StartNew(); while (timer.ElapsedMilliseconds < 15) { Application.DoEvents(); Thread.Sleep(1); } }
    private static void Existing(string method, params object[] arguments)
    { typeof(SecondaryLayoutTests).GetMethod(method, StaticHidden).Invoke(null, arguments); }
    [STAThread] private static int Main()
    {
        string root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "results", "dialog"); Directory.CreateDirectory(root);
        Application.EnableVisualStyles(); EmbeddedDependencyLoader.Register();
        try
        {
            foreach (string language in new[] { "zh-CN", "en-US" })
            foreach (float scale in new[] { 1F, 1.25F, 1.5F, 2F })
            {
                Localizer.SetLanguage(language);
                string context = language + " " + scale;
                typeof(SecondaryLayoutTests).GetField("context", StaticHidden).SetValue(null, context);
                CrosshairOptions source = new CrosshairOptions { Style = CrosshairStyle.CornerCutSquare, Gap = 17 };
                using (CrosshairSettingsDialog dialog = new CrosshairSettingsDialog(source, null, null))
                {
                    ComboBox style = Field<ComboBox>(dialog, "style"); NumericUpDown gap = Field<NumericUpDown>(dialog, "gap");
                    Label gapLabel = Field<Label>(dialog, "gapLabel");
                    Check(style.Items.Count == 9 && style.SelectedIndex == 8, "new style selectable and restored " + context);
                    Check(gap.Enabled && gapLabel.Text == (language == "en-US" ? "Corner gap (px)" : "四角缺口"), "corner-specific gap label " + context);
                    Check(!Field<NumericUpDown>(dialog, "dot").Enabled && Field<NumericUpDown>(dialog, "line").Enabled, "only relevant shape fields enabled " + context);
                    style.SelectedIndex = (int)CrosshairStyle.Cross;
                    Check(gapLabel.Text == (language == "en-US" ? "Center gap (px)" : "中心间隙") && gap.Value == 17, "switching styles restores center wording without losing gap " + context);
                    style.SelectedIndex = (int)CrosshairStyle.CornerCutSquare;
                    gap.Value = 19;
                    Check(source.Gap == 17 && source.Style == CrosshairStyle.CornerCutSquare, "draft leaves source unchanged " + context);
                    dialog.StartPosition = FormStartPosition.Manual; dialog.Location = new Point(-20000, -20000); dialog.ShowInTaskbar = false;
                    Size requested = dialog.ClientSize;
                    if (scale != 1F)
                    {
                        dialog.Scale(new SizeF(scale, scale));
                        Control[] controls = new[] { (Control)dialog }.Concat(All(dialog)).ToArray();
                        Font[] fonts = controls.Select(c => new Font(c.Font.FontFamily, c.Font.Size * scale, c.Font.Style)).ToArray();
                        for (int i = 0; i < controls.Length; i++) controls[i].Font = fonts[i];
                        dialog.Disposed += delegate { foreach (Font font in fonts) font.Dispose(); };
                        requested = new Size((int)(requested.Width * scale), (int)(requested.Height * scale)); dialog.ClientSize = requested;
                    }
                    dialog.Show(); Pump();
                    Size chrome = new Size(dialog.Width - dialog.ClientSize.Width, dialog.Height - dialog.ClientSize.Height);
                    Existing("SyntheticSize", dialog, new Size(requested.Width + chrome.Width, requested.Height + chrome.Height));
                    Existing("Audit", dialog); Existing("ExerciseChoice", style);
                    Check(TextRenderer.MeasureText(style.Text, style.Font).Width + SystemInformation.VerticalScrollBarWidth + 12 <= style.Width, "selected style is readable " + context);
                    Check(TextRenderer.MeasureText(gapLabel.Text, gapLabel.Font).Width <= gapLabel.Width + 3, "corner gap label not clipped " + context);
                    Size original = dialog.Size;
                    Existing("SyntheticSize", dialog, new Size((int)(original.Width * .90), (int)(original.Height * .90)));
                    Pump(); Existing("Audit", dialog);
                    Existing("SyntheticSize", dialog, original); Pump(); Existing("Audit", dialog);
                    using (Bitmap image = new Bitmap(dialog.Width, dialog.Height))
                    { dialog.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size)); image.Save(Path.Combine(root, "corner-" + language + "-" + (int)(scale * 100) + ".png")); }
                    ((Button)dialog.AcceptButton).PerformClick();
                    Check(dialog.Result != null && dialog.Result.Style == CrosshairStyle.CornerCutSquare && dialog.Result.Gap == 19, "saving captures new style and corner gap " + context);
                }
            }
            object failures = typeof(SecondaryLayoutTests).GetField("failures", StaticHidden).GetValue(null);
            string[] issues = ((System.Collections.Generic.IEnumerable<string>)failures).ToArray();
            Check(issues.Length == 0, string.Join(Environment.NewLine, issues));
            int layoutChecks = (int)typeof(SecondaryLayoutTests).GetField("checks", StaticHidden).GetValue(null);
            string summary = "PASS corner-cut dialog: " + checks + " interaction checks + " + layoutChecks + " existing layout checks; zh/en; 100/125/150/200%; scaled fonts; default/compact/restored.";
            File.WriteAllText(Path.Combine(root, "summary.txt"), summary); Console.WriteLine(summary); return 0;
        }
        catch (Exception ex) { File.WriteAllText(Path.Combine(root, "failure.txt"), ex.ToString()); Console.Error.WriteLine(ex); return 1; }
    }
    private static System.Collections.Generic.IEnumerable<Control> All(Control parent)
    { foreach (Control child in parent.Controls) { yield return child; foreach (Control nested in All(child)) yield return nested; } }
}