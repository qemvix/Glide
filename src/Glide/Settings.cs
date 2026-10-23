using System;
using System.IO;
using System.Xml.Serialization;
namespace Glide {
public class Settings {
 public bool Enabled=true; public bool NativeTouchpad=true;
 public int GlideMilliseconds=320,SpeedPercent=100;
 public bool PauseFullscreen=true;
 public string ExcludedApps="";
 public Settings Copy() { return (Settings)MemberwiseClone(); }
 public void Validate() {
  GlideMilliseconds=Math.Max(100,Math.Min(700,GlideMilliseconds));
  SpeedPercent=Math.Max(25,Math.Min(250,SpeedPercent));
  ExcludedApps=ExcludedApps??"";
 }
}
internal static class SettingsStore {
 internal static readonly string Folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"GlideScroll");
 internal static readonly string FileName=Path.Combine(Folder,"settings.xml");
 internal static Settings Load() {
  try { using(FileStream stream=File.OpenRead(FileName)) {
   Settings settings=(Settings)new XmlSerializer(typeof(Settings)).Deserialize(stream); settings.Validate(); return settings;
  }} catch { return new Settings(); }
 }
 internal static void Save(Settings settings) {
  Directory.CreateDirectory(Folder); string temporary=FileName+".tmp";
  using(FileStream stream=File.Create(temporary)) new XmlSerializer(typeof(Settings)).Serialize(stream,settings);
  if(File.Exists(FileName)) File.Replace(temporary,FileName,null); else File.Move(temporary,FileName);
 }
}
// Finite, overlapping ease-out pulses preserve distance and avoid an endless tail.
internal sealed class Motion {
 private sealed class Pulse { internal double Amount,Start,Duration,Progress; }
 private readonly System.Collections.Generic.List<Pulse> pulses=new System.Collections.Generic.List<Pulse>();
 private double carry; private int direction;
 internal bool Active { get { return pulses.Count!=0||Math.Abs(carry)>=0.5; } }
 internal void Clear() { pulses.Clear(); carry=0; direction=0; }
 internal void Add(double amount,double now,int duration) {
  int next=Math.Sign(amount); if(next==0)return;
  if(direction!=0&&direction!=next)Clear(); direction=next;
  if(pulses.Count>=512) { pulses[pulses.Count-1].Amount+=amount; return; }
  pulses.Add(new Pulse{Amount=amount,Start=now,Duration=duration});
 }
 internal int Step(double now) {
  for(int i=pulses.Count-1;i>=0;i--) {
   Pulse pulse=pulses[i]; double t=Math.Max(0,Math.Min(1,(now-pulse.Start)/pulse.Duration));
   double eased=1-Math.Pow(1-t,3); carry+=pulse.Amount*(eased-pulse.Progress); pulse.Progress=eased;
   if(t>=1)pulses.RemoveAt(i);
  }
  int output=pulses.Count==0?(int)Math.Round(carry,MidpointRounding.AwayFromZero):(int)carry;
  output=Math.Max(-30000,Math.Min(30000,output)); carry-=output;
  if(pulses.Count==0&&Math.Abs(carry)<0.5)carry=0; return output;
 }
}
}