using DesktopWindowManager.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vanara.PInvoke;
using WindowsDesktop;

[assembly: InternalsVisibleTo("AmethystWindowsSystrayTests")]
namespace AmethystWindowsSystray
{
    partial class DesktopWindowsManager
    {
        private Dictionary<Pair<VirtualDesktop, HMONITOR>, Layout> Layouts;
        public Dictionary<Pair<VirtualDesktop, HMONITOR>, ObservableCollection<DesktopWindow>> Windows { get; }
        public Dictionary<Pair<VirtualDesktop, HMONITOR>, bool> WindowsSubscribed = new Dictionary<Pair<VirtualDesktop, HMONITOR>, bool>();

        public event EventHandler<string> Changed;
        private Dictionary<Pair<VirtualDesktop, HMONITOR>, int> Factors;

        private readonly string[] FixedFilters = new string[] {
            "Amethyst Windows",
            "AmethystWindowsPackaging",
            "Cortana",
            "Microsoft Spy++",
            "Task Manager",
        };

        public List<Pair<string, string>> ConfigurableFilters = new List<Pair<string, string>>();

        private int padding;

        public int Padding
        {
            get { return padding; }
            set
            {
                padding = value;
                Draw();
            }
        }

        private int marginTop;

        public int MarginTop
        {
            get { return marginTop; }
            set
            {
                marginTop = value;
                Draw();
            }
        }

        private int marginBottom;

        public int MarginBottom
        {
            get { return marginBottom; }
            set
            {
                marginBottom = value;
                Draw();
            }
        }

        private int marginLeft;

        public int MarginLeft
        {
            get { return marginLeft; }
            set
            {
                marginLeft = value;
                Draw();
            }
        }

        private int marginRight;

        public int MarginRight
        {
            get { return marginRight; }
            set
            {
                marginRight = value;
                Draw();
            }
        }

        private int layoutPadding;

        public int LayoutPadding
        {
            get { return layoutPadding; }
            set
            {
                layoutPadding = value;
                Draw();
            }
        }

        private bool disabled;

        public bool Disabled
        {
            get { return disabled; }
            set
            {
                disabled = value;
            }
        }

        public DesktopWindowsManager()
        {
            this.padding = Properties.Settings.Default.Padding;
            this.marginTop = Properties.Settings.Default.MarginTop;
            this.marginBottom = Properties.Settings.Default.MarginBottom;
            this.marginLeft = Properties.Settings.Default.MarginLeft;
            this.marginRight = Properties.Settings.Default.MarginRight;
            this.layoutPadding = Properties.Settings.Default.LayoutPadding;
            this.ConfigurableFilters = JsonConvert.DeserializeObject<List<Pair<string, string>>>(Properties.Settings.Default.Filters);
            this.Layouts = new Dictionary<Pair<VirtualDesktop, HMONITOR>, Layout>();
            this.Windows = new Dictionary<Pair<VirtualDesktop, HMONITOR>, ObservableCollection<DesktopWindow>>();
            this.Factors = new Dictionary<Pair<VirtualDesktop, HMONITOR>, int>();
        }

        public void RotateMonitorClockwise(Pair<VirtualDesktop, HMONITOR> currentDesktopMonitor)
        {
            List<HMONITOR> virtualDesktopMonitors = Windows
                .Keys
                .Where(desktopMonitor => desktopMonitor.Item1.Equals(currentDesktopMonitor.Item1))
                .Select(desktopMonitor => desktopMonitor.Item2)
                .ToList();

            HMONITOR nextMonitor = virtualDesktopMonitors.SkipWhile(x => x != currentDesktopMonitor.Item2).Skip(1).DefaultIfEmpty(virtualDesktopMonitors[0]).FirstOrDefault();
            Pair<VirtualDesktop, HMONITOR> nextDesktopMonitor = new Pair<VirtualDesktop, HMONITOR>(currentDesktopMonitor.Item1, nextMonitor);

            User32.SetForegroundWindow(Windows[nextDesktopMonitor].FirstOrDefault().Window);
        }

        public void RotateMonitorCounterClockwise(Pair<VirtualDesktop, HMONITOR> currentDesktopMonitor)
        {
            List<HMONITOR> virtualDesktopMonitors = Windows
                .Keys
                .Where(desktopMonitor => desktopMonitor.Item1.Equals(currentDesktopMonitor.Item1))
                .Select(desktopMonitor => desktopMonitor.Item2)
                .ToList();

            HMONITOR nextMonitor = virtualDesktopMonitors.TakeWhile(x => x != currentDesktopMonitor.Item2).Skip(1).DefaultIfEmpty(virtualDesktopMonitors[0]).FirstOrDefault();
            Pair<VirtualDesktop, HMONITOR> nextDesktopMonitor = new Pair<VirtualDesktop, HMONITOR>(currentDesktopMonitor.Item1, nextMonitor);

            User32.SetForegroundWindow(Windows[nextDesktopMonitor].FirstOrDefault().Window);
        }

        public void LoadFactors()
        {
            if (Properties.Settings.Default.Factors != "")
            {
                ReadFactors();
            }
        }

        public void SaveFactors()
        {
            Properties.Settings.Default.Factors = JsonConvert.SerializeObject(Factors.ToList(), Formatting.Indented, new FactorsConverter());
            Properties.Settings.Default.Save();
        }

        public void ReadFactors()
        {
            Factors = JsonConvert.DeserializeObject<List<KeyValuePair<Pair<VirtualDesktop, HMONITOR>, int>>>(
                Properties.Settings.Default.Factors.ToString(), new FactorsConverter()
                ).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public void UpdateFactors()
        {
            foreach (Pair<VirtualDesktop, HMONITOR> desktopMonitor in Windows.Keys)
            {
                if (!Factors.ContainsKey(desktopMonitor))
                {
                    Factors.Add(desktopMonitor, 0);
                }
            }
        }

        private void SubscribeWindowsCollectionChanged(Pair<VirtualDesktop, HMONITOR> desktopMonitor, bool enabled)
        {
            if (!enabled) Windows[desktopMonitor].CollectionChanged -= Windows_CollectionChanged;
            else if (!WindowsSubscribed[desktopMonitor]) Windows[desktopMonitor].CollectionChanged += Windows_CollectionChanged;

            WindowsSubscribed[desktopMonitor] = enabled;
        }

    }
}
