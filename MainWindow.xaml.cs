using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;
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
    public const int MachineQuantityMaxConst = 100;
    public const int TimeScaleFactorMax = 1000;
    const double MaxDelayForOneStep = 1;

    private Thread _thread;
    private volatile bool _threadStarted;
    private int MachineQuantityMax = MachineQuantityMaxConst;

    public volatile Hashtable StatesHashtable;

    public MainWindow()
    {
      InitializeComponent();
      LoadDicts();

      //string ip = "192.168.9.11";  // mes8a
      string ip = "192.168.3.27";  // mes8n
      TextBoxHydraHost.Text = padIP(ip);
    }

    private string padIP(string ip)
    {
      string output = string.Empty;
      string[] parts = ip.Split('.');
      for (int i = 0; i < parts.Length; i++)
      {
        output += parts[i].PadLeft(3, '0');
        if (i != parts.Length - 1)
          output += ".";
      }
      return output;
    }

    // dicts
    private void LoadDicts()
    {
      XmlReader xmlReader;
      XElement root;
      int i, i2;

      // Популируем таблицу DataGridMachines
      //DataGridMachines.ItemsSource = Machine.GetMachines();
      xmlReader = XmlReader.Create(AppDomain.CurrentDomain.BaseDirectory + "Custom\\Machines.xml");
      root = XElement.Load(xmlReader);
      IEnumerable<XElement> machines = root.Elements("machine");
      Dictionary<string, string> machinesMap = machines.ToDictionary(
      element => element.Attribute("mnr").Value, // Key selector
      element => element.Value);
      var ocMachines = new ObservableCollection<Machine>();
      foreach (KeyValuePair<string, string> pair in machinesMap)
      {
        ocMachines.Add(new Machine() { Mnr = pair.Key, Name = pair.Value });
      }
      DataGridMachines.ItemsSource = ocMachines;

      MachineQuantityMax = DataGridMachines.Items.Count;      // переопределяется по фактическому наличию

      // Популируем таблицу DataGridStates1
      // DataGridStates.ItemsSource = State.GetStates();
      xmlReader = XmlReader.Create(AppDomain.CurrentDomain.BaseDirectory + "Custom\\StatesSequence.xml");
      root = XElement.Load(xmlReader);
      IEnumerable<XElement> stateMsDelays = root.Elements("StateMsDelay");
      var ocStateMsDelays = new ObservableCollection<State>();
      foreach (XElement stateMsDelay in stateMsDelays)
      {
        int.TryParse(stateMsDelay.FirstAttribute.Value, out i);
        int.TryParse(stateMsDelay.Value, out i2);
        ocStateMsDelays.Add(new State() { Status = i, MsDelay = i2 });
      }
      DataGridStates.ItemsSource = ocStateMsDelays;

      // Заполним словарь состояний (StatesHashtable)
      xmlReader = XmlReader.Create(AppDomain.CurrentDomain.BaseDirectory + "Custom\\States.xml");
      root = XElement.Load(xmlReader);
      IEnumerable<XElement> states = root.Elements("state");
      Dictionary<string, string> statesMap = states.ToDictionary(
      element => element.Attribute("id").Value, // Key selector
      element => element.Value);
      Dictionary<int, string> statesMap2 = new Dictionary<int, string>();
      foreach (KeyValuePair<string, string> pair in statesMap)
      {
        bool tryParse = int.TryParse(pair.Key, out i);
        statesMap2.Add(i, pair.Value);
      }
      StatesHashtable = new Hashtable(statesMap2);

      // Пропишем названия статусов в таблице DataGridStates
      for (int j = 0; j < DataGridStates.Items.Count; j++)
      {
        DataGridStates.CurrentItem = DataGridStates.Items[j];
        State s = (State)DataGridStates.CurrentItem;
        s.Designation = StatesHashtable.Contains(s.Status) ? StatesHashtable[s.Status].ToString() : s.Status.ToString();
      }

    }

    // ButtonSpinner SpinnerMachineQuantity
    private volatile bool _smqInKeyProccessing;

    private void SpinnerMachineQuantity_Spin(object sender, SpinEventArgs e)
    {
      _smqInKeyProccessing = true;

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

      _smqInKeyProccessing = false;
    }

    private void SpinnerMachineQuantity_KeyDown(object sender, KeyEventArgs e)
    {
      _smqInKeyProccessing = true;

      const string rStr = "^(D|NumPad)[0-9]$";

      if (e.Key == Key.Tab)
      {
        _smqInKeyProccessing = false;
        return;
      }

      var key = e.Key.ToString();
      var r = new Regex(rStr, RegexOptions.IgnoreCase);
      e.Handled = !r.IsMatch(key);

      _smqInKeyProccessing = false;
    }

    private void SpinnerMachineQuantity_KeyUp(object sender, KeyEventArgs e)
    {
      _smqInKeyProccessing = true;

      var spinner = (ButtonSpinner)sender;
      var txtBox = (TextBox)spinner.Content;

      if (String.IsNullOrEmpty(txtBox.Text))
      {
        _smqInKeyProccessing = false;
        return;
      }

      int i;
      var value = !int.TryParse(txtBox.Text, out i) ? MachineQuantityMax : i;

      if (value == 0 || value == MachineQuantityMax)
      {
        txtBox.Text = value.ToString();
        _smqInKeyProccessing = false;
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

      _smqInKeyProccessing = false;
    }

    // ButtonSpinner SpinnerTimeScaleFactor
    private volatile bool _stsfInKeyProccessing;

    private void SpinnerTimeScaleFactor_Spin(object sender, SpinEventArgs e)
    {
      _stsfInKeyProccessing = true;

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

      _stsfInKeyProccessing = false;
    }

    private void SpinnerTimeScaleFactor_KeyDown(object sender, KeyEventArgs e)
    {
      _stsfInKeyProccessing = true;

      const string rStr = "^((D|NumPad)[0-9]|OemPeriod|Decimal)$";

      var spinner = (ButtonSpinner)sender;
      var txtBox = (TextBox)spinner.Content;

      if (e.Key == Key.Tab)
      {
        _stsfInKeyProccessing = false;
        return;
      }

      var key = e.Key.ToString();
      var r = new Regex(rStr, RegexOptions.IgnoreCase);
      e.Handled = !r.IsMatch(key) || ((key == "OemPeriod" || key == "Decimal") && txtBox.Text.Contains("."));

      _stsfInKeyProccessing = false;
    }

    private void SpinnerTimeScaleFactor_KeyUp(object sender, KeyEventArgs e)
    {
      _stsfInKeyProccessing = true;

      var spinner = (ButtonSpinner)sender;
      var txtBox = (TextBox)spinner.Content;

      if (String.IsNullOrEmpty(txtBox.Text))
      {
        _stsfInKeyProccessing = false;
        return;
      }

      double d;
      var value = !double.TryParse(txtBox.Text, out d) ? TimeScaleFactorMax : d;

      if (value == TimeScaleFactorMax)
      {
        txtBox.Text = value.ToString();
        _stsfInKeyProccessing = false;
        return;
      }

      if (value == 0)
      {
        _stsfInKeyProccessing = false;
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

      _stsfInKeyProccessing = false;
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

    private void WDisp_GetMachineQuantity()
    {
      this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate()
      {
        mq = _MachineQuantity();
      });
    }

    private void WDisp_GetTimeScaleFactor()
    {
      this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate()
      {
        tsf = _TimeScaleFactor();
      });
    }

    private volatile bool _isSpinnerInKeyProccessing;
    private bool WDisp_IsSpinnerInKeyProccessing()
    {
      this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate()
        {
          _isSpinnerInKeyProccessing = _smqInKeyProccessing || _stsfInKeyProccessing;
        });
      return _isSpinnerInKeyProccessing;
    }

    private volatile int _machineQuantityNew;
    private double _timeScaleFactorNew;
    private bool WDisp_IsData1Changed()
    {
      this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate()
      {
        if (_smqInKeyProccessing || _stsfInKeyProccessing)
        {
          // до момента окончания обработки спиновых полей считаем, что изменений нет
          _machineQuantityNew = mq;
          _timeScaleFactorNew = tsf;
        }
        else
        {
          _machineQuantityNew = _MachineQuantity();
          _timeScaleFactorNew = _TimeScaleFactor();
        }
      });
      return _machineQuantityNew != mq || _timeScaleFactorNew != tsf;
    }

    private bool WDisp_IsMachineQuantityChanged()
    {
      this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate()
      {
        if (_smqInKeyProccessing)
        {
          // до момента окончания обработки спиновых полей считаем, что изменений нет
          _machineQuantityNew = mq;
        }
        else
          _machineQuantityNew = _MachineQuantity();
      });
      return _machineQuantityNew != mq;
    }

    private bool WDisp_IsTimeScaleFactorChanged()
    {
      this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate()
      {
        if (_stsfInKeyProccessing)
        {
          // до момента окончания обработки спиновых полей считаем, что изменений нет
          _timeScaleFactorNew = tsf;
        }
        else
          _timeScaleFactorNew = _TimeScaleFactor();
      });
      return _timeScaleFactorNew != tsf;
    }

    private void WDisp_gridSetMachineStatus(int rowIdx, State state)
    {
      this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate()
        {
          //DataGridMachines.SelectedItem = DataGridMachines.Items[rowIdx];
          //Machine m = (Machine)DataGridMachines.SelectedItem;
          // DataGridMachines.Focus();
          DataGridMachines.CurrentItem = DataGridMachines.Items[rowIdx];
          Machine m = (Machine)DataGridMachines.CurrentItem;
          m.Status = state.Status;
          m.Designation = StatesHashtable.Contains(state.Status) ? StatesHashtable[state.Status].ToString() : m.Status.ToString();
          m.MsDelay = state.MsDelay;
          m.MsInStatus = 0;
        });
    }

    private void WDisp_gridForAllMachinesAddToMsInStatus(int msAdd)
    {
      this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate()
      {
        for (int i = 0; i < mq; i++)
        {
          DataGridMachines.CurrentItem = DataGridMachines.Items[i];
          Machine m = (Machine)DataGridMachines.CurrentItem;
          m.MsInStatus = (m.MsInStatus + msAdd > m.MsDelay) ? m.MsDelay : m.MsInStatus + msAdd;
        }
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
        if (!_threadStarted) break;

        if (WDisp_IsSpinnerInKeyProccessing()) break;
        WDisp_GetData1();
        if (mq <= 0 || mq > MachineQuantityMax) break;
        if (tsf < 0.001 || tsf > TimeScaleFactorMax) break;

        var stateIdx = new long[mq];

        // пропишем индексы начальных состояний (пока берём просто со смещением 1 - потом можно сделать параметр настройки)
        int j = 0;
        for (int i = 0; i < mq; i++)
        {
          var stateItem = (State)stateItemCollection[j];
          stateIdx[i] = make3(i, j, stateItem.MsDelay);
          WDisp_gridSetMachineStatus(i, (State)stateItem);   // в таблице установим новый статус
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

            // "мягкое" изменение - просто подменим коэффициент
            if (WDisp_IsTimeScaleFactorChanged())
            {
              double tsfPre = tsf;
              WDisp_GetTimeScaleFactor();
              if (tsf < 0.001 || tsf > TimeScaleFactorMax) break;  // !!!
              ssScaled = ss * tsf;
              ssToDelay = ssToDelay * tsf / tsfPre;
            }
            if (tsf < 0.001 || tsf > TimeScaleFactorMax) break;  // !!!

            // "жёсткое" изменение - надо выходить на верхний уровень цикла (с переинициализацией)
            if (WDisp_IsMachineQuantityChanged())
              break;

            double ssToDelayStep = MaxDelayForOneStep > ssToDelay ? ssToDelay : MaxDelayForOneStep;
            Thread.Sleep(TimeSpan.FromSeconds(ssToDelayStep));
            ssToDelay -= ssToDelayStep;
            WDisp_gridForAllMachinesAddToMsInStatus((int)(MaxDelayForOneStep * 1000 / tsf));
          }
          // запоминаем момент времени (для коррекции следующей задержки)
          ticks = DateTime.Now.Ticks;

          if (!_threadStarted) break;

          // "жёсткое" изменение - надо выходить на верхний уровень цикла (с переинициализацией)
          if (WDisp_IsMachineQuantityChanged())
            break;

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
              WDisp_gridSetMachineStatus(m1, (State)stateItem);   // в таблице установим новый статус
            }
            else
              break;
          }

          Array.Sort(stateIdx);
        }
        if (!_threadStarted) break;

      }
    }

    private void DataGridMachines_AutoGeneratedColumns(object sender, EventArgs e)
    {
      DataGridMachines.Columns[1].Width = 100; // DataGridLength.SizeToHeader;
      DataGridMachines.Columns[2].Width = DataGridLength.SizeToHeader;
    }

    private void DataGridStates_AutoGeneratedColumns(object sender, EventArgs e)
    {
      DataGridStates.Columns[1].Width = 120; // DataGridLength.SizeToHeader;
    }

    private void mwStressTest_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      if (_threadStarted)
        ButtonStop_Click(null, null);
    }

    private void TextBoxHydraHost_KeyUp(object sender, KeyEventArgs e)
    {
        var mtb = (MaskedTextBox)sender;
        //var str = mtb.MaskedTextProvider.ToString();
        var str = mtb.MaskedTextProvider.ToDisplayString();
        int selectionStart = mtb.SelectionStart;
        int selectionLength = mtb.SelectionLength;
        str = str.Replace("_", "0");
        TextBoxHydraHost.Text = str;
        mtb.SelectionStart = selectionStart;
        mtb.SelectionLength = selectionLength;
    }

  }
}
