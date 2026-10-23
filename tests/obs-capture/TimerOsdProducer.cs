using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using InputStitch;
internal static class TimerOsdProducer
{
 [DllImport("user32.dll")] private static extern bool SetProcessDpiAwarenessContext(IntPtr context);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] private static extern IntPtr FindWindow(string cls,string title);
 [DllImport("user32.dll")] private static extern bool ClientToScreen(IntPtr hwnd,ref Point point);
 [STAThread] private static int Main()
 {
  SetProcessDpiAwarenessContext(new IntPtr(-4));Application.EnableVisualStyles();
  string root=AppDomain.CurrentDomain.BaseDirectory;string command=Path.Combine(root,"detours-producer-command.txt");
  Point origin=Point.Empty;IntPtr target=FindWindow("InputStitchNativeD3D9TargetClass","InputStitch Native D3D9 Target");
  if(target==IntPtr.Zero || !ClientToScreen(target,ref origin))return 2;
  using(Form owner=new Form {ShowInTaskbar=false})
  using(TimerController rtss=new TimerController(owner))
  using(TimerController desktop=new TimerController(owner))
  {
   IntPtr ignored=owner.Handle;
   rtss.Apply(Parts(new TimerOptions {Enabled=true,Visible=false,Backend=TimerBackend.Rtss,FontSize=36,Color="#FFE020",X=40,Y=60,TextTemplate="RTSS 录入 {time}",Bold=true}));
   desktop.Apply(Parts(new TimerOptions {Enabled=true,Visible=false,Backend=TimerBackend.Desktop,FontSize=36,Color="#FF28DF",X=origin.X+40,Y=origin.Y+230,TextTemplate="DESKTOP 顶层 {time}",Bold=true}));
   rtss.Start();desktop.Start();Stopwatch clock=Stopwatch.StartNew();long nextRead=0,nextStatus=0;string mode="off";
   File.WriteAllText(Path.Combine(root,"detours-producer-ready.json"),"{\"pid\":"+Process.GetCurrentProcess().Id+",\"clientX\":"+origin.X+",\"clientY\":"+origin.Y+"}");
   while(clock.ElapsedMilliseconds<1200000)
   {
    if(clock.ElapsedMilliseconds>=nextRead)
    {
     nextRead=clock.ElapsedMilliseconds+100;
     if(File.Exists(command))
     {
      string next=File.ReadAllText(command).Trim();if(next=="stop")break;
      if(next!=mode){mode=next;rtss.SetVisible(mode=="both"||mode=="rtss");desktop.SetVisible(mode=="both"||mode=="desktop");}
     }
    }
    Application.DoEvents();desktop.KeepOnTop();
    if(clock.ElapsedMilliseconds>=nextStatus){nextStatus=clock.ElapsedMilliseconds+1000;File.WriteAllText(Path.Combine(root,"detours-producer-status.json"),"{\"mode\":\""+mode+"\",\"rtssStatus\":\""+rtss.Status+"\",\"elapsedMs\":"+(long)rtss.Elapsed.TotalMilliseconds+"}");}
    Thread.Sleep(15);
   }
  }
  return 0;
 }
 private static TimerOptions Parts(TimerOptions options)
 {
  options.Parts=TimerText.CreateParts(options);
  foreach(TimerPart part in options.Parts)if(part.Kind==TimerPartKind.Milliseconds){part.Style.FontSize=19;part.Style.Italic=true;}
  return options;
 }
}
