using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.IO;

namespace StaMonitor
{
  /// <summary>
  /// MainWindow.xaml の相互作用ロジック
  /// </summary>
  public partial class MainWindow : Window
  {
    public double _end_time = 0;

    private bool _isRunning = false;

    /// <summary>
    ///  Interval of Pooling sta file (unit: milli second)
    /// </summary>
    public int PoolingInterval { get; set; } = 100;

    private FileInfo target;
    private Stack<string> lines = new Stack<string>();

    public MainWindow() {
      InitializeComponent();
      var name = getTargetFileName();
      if (name != "") {
        target = new FileInfo(name);
        var inp = Path.ChangeExtension(name, ".inp");
        var counter = new InpStepCounter(inp);
        EndTime.Text = counter.TotalTime.ToString();
      }
    }

    private void EndTime_TextChanged(object sender, TextChangedEventArgs e) {
      double value;
      if(double.TryParse(EndTime.Text,out value)) {
        _end_time = value;
      }
    }

    private string getTargetFileName() {
      string[] args = Environment.GetCommandLineArgs();
      if (args.Length == 1) {
        var dlg = new Microsoft.Win32.OpenFileDialog();
        dlg.CheckFileExists = true;
        dlg.CheckPathExists = true;
        dlg.Filter = "Sta file(*.sta)|*.sta";
        dlg.Multiselect = false;
        dlg.DefaultExt = ".sta";
        dlg.Title = "Select Sta file";
        if (dlg.ShowDialog() ?? false) {
          return dlg.FileName;
        } else {
          return "";
        }
      } else {
        return args[1];
      }
    }

    private async void button_Click(object sender, RoutedEventArgs e) {
      this.WindowState = WindowState.Minimized;

      // if already running, just minimize.
      if (_isRunning)
        return;

      _isRunning = true;
      // Open with share control because the sta file was opened by abaqus process to write.
      using (var s = target.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) {
        using (var f = new StreamReader(s)) {
          // Read and store all lines of sta file into the 'lines' stack.
          string line = f.ReadLine();
          do {
            lines.Push(line);
            line = f.ReadLine();
          } while (!String.IsNullOrWhiteSpace(line));
          line = lines.Peek();

          // Main loop
          while (!(line ?? "COMPLETED").Contains("COMPLETED")) {
            var arr = line.Split(null).Where((str) => str.Length > 0).ToArray();
            if (arr.Count() > 1) {
              var time = arr[6];
              var delta = arr[8];
              double time_value = 0.0;
              if (double.TryParse(time, out time_value)) {
                var rate = 100.0 * double.Parse(time) / _end_time;
                Title = $"{time}/{_end_time}({rate,4:0.0}%)[{delta}]";
              }
              UpdateTail();
            }

            // Read Next line with waiting
            line = await Task.Run(() => {
              while (f.EndOfStream) {
                System.Threading.Thread.Sleep(PoolingInterval);
              }
              return f.ReadLine();
            });
            lines.Push(line);
          }
          UpdateTail();
          Title = "Finished.";
        }
      }
      this.WindowState = WindowState.Normal;
      this.Activate();
    }

    /// <summary>
    /// Update the display of the last some lines of the sta file.
    /// </summary>
    private void UpdateTail() {
      textBlock.Text = String.Join("\n", lines.Take(5).Reverse());
    }


    private void Window_Activated(object sender, EventArgs e) {
      if (target == null) {
        this.Close();
      }
    }
  }
}
