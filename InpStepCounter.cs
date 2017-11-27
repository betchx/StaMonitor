using System.Collections.Generic;
using System.IO;

namespace StaMonitor
{
  /// <summary>
  /// Read an Abaqus input file and count the total/step time of all steps.
  /// </summary>
  class InpStepCounter
  {
    private List<double> _times = new List<double>();

    public InpStepCounter(string filename) {
      FileName = filename;
      TotalTime = 0.0;
      Read();
    }

    private void Read() {
      if (!File.Exists(FileName))
        return;
      using(var f = new StreamReader(FileName)) {
        while(!f.EndOfStream) {
          var line = f.ReadLine().ToUpper();
          if (line.StartsWith("*STATIC") || line.StartsWith("*DYNAMIC")) {
            // Static
            var data = f.ReadLine().Split(new char[] { ',' });
            double tm;
            if(! double.TryParse(data[1],out tm)) {
              tm = 1.0;
            }
            _times.Add(tm);
            TotalTime += tm;
          }
        }
      }
    }

    public string FileName {
      get;
      private set;
    }
    public double TotalTime {
      get;
      private set;
    }

    public double StepTime(int step) {
      return _times[step];
    }
  }
}
