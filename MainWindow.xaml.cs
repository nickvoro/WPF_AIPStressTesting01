using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Xceed.Wpf.Toolkit;
using System.Text.RegularExpressions;
using MessageBox = Xceed.Wpf.Toolkit.MessageBox;

namespace WPF_AIPStressTesting01
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public const int MachineQuantityMax = 100;
    public const int TimeScaleFactorMax = 1000;
    const double MaxDelayForOneStep = 1;

    private Thread _thread;
    private volatile bool _threadStarted;

    public MainWindow()
    {
      InitializeComponent();
      DataGridMachines.ItemsSource = Machine.GetMachines();
      DataGridStates.ItemsSource = State.GetStates();
    }

    // ButtonSpinner SpinnerMachineQuantity

    private void SpinnerMachineQuantity_Spin(object sender, SpinEventArgs e)
    {
      var spinner = (ButtonSpinner)sender;
      var txtBox = (TextBox)spinner.Content;
      int i;
      int value = String.IsNullOrEmpty(txtBox.Text) || !int.TryParse(txtBox.Text, out i) ? 0 : Convert.ToInt32(txtBox.Text);

      if (e.Direction == SpinDirection.Increase)
        value++;
      else
        value--;

      if (value < 1)
        value = 1;
      else if (value > MachineQuantityMax)
        value = MachineQuantityMax;

      txtBox.Text = value.ToString();
    }

    private void SpinnerMachineQuantity_KeyDown(object sender, KeyEventArgs e)
    {
      const string rStr = "^(D|NumPad)[0-9]$";

      if (e.Key == Key.Tab)
      {
        return;
      }

      var key = e.Key.ToString();
      var r = new Regex(rStr, RegexOptions.IgnoreCase);
      e.Handled = !r.IsMatch(key);
    }

    private void SpinnerMachineQuantity_KeyUp(object sender, KeyEventArgs e)
    {
      var spinner = (ButtonSpinner)sender;
      var txtBox = (TextBox)spinner.Content;

      if (String.IsNullOrEmpty(txtBox.Text))
        return;

      int i;
      var value = !int.TryParse(txtBox.Text, out i) ? MachineQuantityMax : i;

      if (value == 0 || value == MachineQuantityMax)
      {
        txtBox.Text = value.ToString();
        return;
      }

      if (value < 1 || value > MachineQuantityMax)
      {
        if (value < 1)
          value = 1;
        else if (value > MachineQuantityMax)
          value = MachineQuantityMax;
        txtBox.Text = value.ToString();
      }
    }

    // ButtonSpinner SpinnerTimeScaleFactor

    private void SpinnerTimeScaleFactor_Spin(object sender, SpinEventArgs e)
    {
      var spinner = (ButtonSpinner)sender;
      var txtBox = (TextBox)spinner.Content;
      double d;
      var value = String.IsNullOrEmpty(txtBox.Text) || !double.TryParse(txtBox.Text, out d) ? 0 : Convert.ToDouble(txtBox.Text);
      double delta;

      if (e.Direction == SpinDirection.Increase)
      {
        if (value > .009 && value < .01)
          value = .009;
        else if (value > .09 && value < .1)
          value = .09;
        else if (value > .9 && value < 1)
          value = .9;
        delta = value < .01 ? .001 : (value < .1 ? .01 : (value < 1 ? .1 : 1));
        value += delta;
      }
      else
      {
        delta = value <= .01 ? .001 : (value <= .1 ? .01 : (value <= 1 ? .1 : 1));
        value -= delta;
      }

      if (value <= 0)
        value = .001;
      else if (value > TimeScaleFactorMax)
        value = TimeScaleFactorMax;

      txtBox.Text = value.ToString();
    }

    private void SpinnerTimeScaleFactor_KeyDown(object sender, KeyEventArgs e)
    {
      const string rStr = "^((D|NumPad)[0-9]|OemPeriod|Decimal)$";

      var spinner = (ButtonSpinner)sender;
      var txtBox = (TextBox)spinner.Content;

      if (e.Key == Key.Tab)
      {
        return;
      }

      var key = e.Key.ToString();
      var r = new Regex(rStr, RegexOptions.IgnoreCase);
      e.Handled = !r.IsMatch(key) || ((key == "OemPeriod" || key == "Decimal") && txtBox.Text.Contains("."));
    }

    private void SpinnerTimeScaleFactor_KeyUp(object sender, KeyEventArgs e)
    {
      var spinner = (ButtonSpinner)sender;
      var txtBox = (TextBox)spinner.Content;

      if (String.IsNullOrEmpty(txtBox.Text))
        return;

      double d;
      var value = !double.TryParse(txtBox.Text, out d) ? TimeScaleFactorMax : d;

      if (value == TimeScaleFactorMax)
      {
        txtBox.Text = value.ToString();
        return;
      }

      if (value == 0)
      {
        return;
      }

      if (value <= 0 || value > TimeScaleFactorMax)
      {
        if (value <= 0)
          value = .1;
        else if (value > TimeScaleFactorMax)
          value = TimeScaleFactorMax;
        txtBox.Text = value.ToString();
      }
    }

    // DataGridMachines
    private void DataGridMachines_LoadingRow(object sender, DataGridRowEventArgs e)
    {
      var rowCnt = e.Row.GetIndex() + 1;
      e.Row.Header = rowCnt.ToString();
    }

    // DataGridStates
    private void DataGridStates_LoadingRow(object sender, DataGridRowEventArgs e)
    {
      var rowCnt = e.Row.GetIndex() + 1;
      e.Row.Header = rowCnt.ToString();
    }

    private void ButtonStart_Click(object sender, RoutedEventArgs e)
    {
      int mq = _MachineQuantity();
      if (mq <= 0)
      {
        MessageBox.Show("Parameter Machine Quantity is not setted!");
        return;
      }

      double tsf = _TimeScaleFactor();
      if (tsf <= 0)
      {
        MessageBox.Show("Parameter Time Scale Factor is not setted!");
        return;
      }

      _thread = new Thread(Test);
      _threadStarted = true;
      _thread.Start();
      ButtonStart.IsEnabled = false;
      ButtonStop.IsEnabled = true;
    }

    private void ButtonStop_Click(object sender, RoutedEventArgs e)
    {
      _threadStarted = false;
      ButtonStop.IsEnabled = false;
      _NestedEmptyMessageLoop();
      while (_thread != null && _thread.Join(2000)) { _thread.Interrupt(); _thread = null; }
      ButtonStart.IsEnabled = true;
    }

    // used functions

    // see about trick at link: http://www.codeproject.com/Articles/271598/Application-DoEvents-in-WPF
    private void _NestedEmptyMessageLoop()
    {
      Application.Current.Dispatcher.Invoke(
        DispatcherPriority.Background,
        new ThreadStart(delegate { })
        );
    }
    private int _MachineQuantity()
    {
      var spinner = SpinnerMachineQuantity;
      var txtBox = (TextBox)spinner.Content;
      int i;
      var value = !int.TryParse(txtBox.Text, out i) ? 0 : i;
      return value;
    }
    private double _TimeScaleFactor()
    {
      var spinner = SpinnerTimeScaleFactor;
      var txtBox = (TextBox)spinner.Content;
      double d;
      var value = !double.TryParse(txtBox.Text, out d) ? 0 : d;
      return value;
    }

    private volatile ItemCollection machineItemCollection;
    private volatile ItemCollection stateItemCollection;
    private volatile int mq;
    private volatile int sq;
    private double tsf;

    private void WDisp_GetData()
    {
      this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate()
      {
        machineItemCollection = DataGridMachines.Items;
        stateItemCollection = DataGridStates.Items;
        sq = DataGridStates.Items.Count;
      });
    }

    private void WDisp_GetData1()
    {
      this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate()
      {
        mq = _MachineQuantity();
        tsf = _TimeScaleFactor();
      });
    }

    private long make3(int machineIdx, int stateIdx, int msDelay)
    {
      return machineIdx + 1000 * stateIdx + (long)1000000 * msDelay;
    }
    private int MachineIdxFrom3(long l3)
    {
      return (int)(l3 % 1000);
    }
    private int StateIdxFrom3(long l3)
    {
      return (int)((l3 % 1000000) / 1000);
    }
    private int MsDelayFrom3(long l3)
    {
      return (int)(l3 / 1000000);
    }

    private void Test()
    {
      WDisp_GetData();
      while (_threadStarted)
      {
        //Thread.Sleep(TimeSpan.FromSeconds(1));
        if (!_threadStarted) break;
        WDisp_GetData1();

        var stateIdx = new long[mq];

        // пропишем индексы начальных состояний (пока берём просто со смещением 1 - потом можно сделать параметр настройки)
        int j = 0;
        for (int i = 0; i < mq; i++)
        {
          var stateItem = (State)stateItemCollection[j];
          stateIdx[i] = make3(i, j, stateItem.MsDelay);
          j++;
          if (j >= sq)
            j = 0;
        }
        Array.Sort(stateIdx);

        // запоминаем момент отсчёта времени (для коррекции следующей задержки)
        long ticks = DateTime.Now.Ticks;

        // цикл по массиву stateIdx
        while (_threadStarted)
        {
          int m = MachineIdxFrom3(stateIdx[0]);
          int s = StateIdxFrom3(stateIdx[0]);
          int d = MsDelayFrom3(stateIdx[0]);

          // для всех элементов в массиве stateIdx вычтем минимальную задержку
          for (int i = 0; i < mq; i++)
          {
            stateIdx[i] = make3(MachineIdxFrom3(stateIdx[i]), StateIdxFrom3(stateIdx[i]), MsDelayFrom3(stateIdx[i]) - d);
          }

          if (!_threadStarted)
            break;

          // отрабатываем задержку
          long ticks2 = DateTime.Now.Ticks;
          long diffTicks = d - (ticks2 - ticks) / TimeSpan.TicksPerMillisecond;
          if (diffTicks < 0) diffTicks = 0;
          double ss = (double)diffTicks / 1000;
          double ssScaled = ss * tsf;
          double ssToDelay = ssScaled;
          while (ssToDelay > 0)
          {
            if (!_threadStarted) break;
            double ssToDelayStep = MaxDelayForOneStep > ssToDelay ? ssToDelay : MaxDelayForOneStep;
            Thread.Sleep(TimeSpan.FromSeconds(ssToDelayStep));
            ssToDelay -= ssToDelayStep;
          }
          // запоминаем момент времени (для коррекции следующей задержки)
          ticks = DateTime.Now.Ticks;

          // для первых эелементов массива, у которых задержка стала = 0, отрабатываем команду
          // и прописываем следующее состояние
          for (int i = 0; i < mq; i++)
          {
            int m1 = MachineIdxFrom3(stateIdx[i]);
            int s1 = StateIdxFrom3(stateIdx[i]);
            int d1 = MsDelayFrom3(stateIdx[i]);

            if (d1 == 0)
            {
              if (!_threadStarted)
                break;

              // TODO - шлём команду на сервер
              // ...

              // прописываем следующее состояние
              s1++;
              if (s1 >= sq)
                s1 = 0;
              var stateItem = (State)stateItemCollection[s1];
              stateIdx[i] = make3(m1, s1, stateItem.MsDelay);
            }
            else
              break;
          }

          Array.Sort(stateIdx);
        }

      }
    }
  }
}
