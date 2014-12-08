using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data.Linq;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
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
    public enum StatusProcessingType
    {
      /// <summary>не обработано</summary>
      NotProcessed = 0,
      /// <summary>отложенная обработка</summary>
      NotProcessedIdle = 1,
      NotProcessedWithPccDif = 2,
      Processed = 3,
      ProcessedTryPccDif = 4,
      ProcessedWithPccDif = 5,
      ProcessedWithoutPccDif = 6,
      StatusExists = 7,
      //StatusRewritten = 7,
      StatusExpired = 8,
      //StatusPccDifExpired = 9,
      ErrorOnSend = 9
    }

    public const int MachineQuantityMaxConst = 100;
    public const int TimeScaleFactorMax = 1000;
    const double MaxDelayForOneStep = 1;
    public const int PrognameDelayMaxConst = 1000000;

    private Thread _thread;
    private volatile bool _threadStarted;
    private int MachineQuantityMax = MachineQuantityMaxConst;
    private int PrognameDelayMax = PrognameDelayMaxConst;

    public volatile Hashtable StatesHashtable;

    private DataClasses1DataContext db;
    private DataClasses2DataContext db2;

    private volatile int _serviceTimerInterval;
    private volatile int _serviceLastProcId;

    public MainWindow()
    {
      InitializeComponent();
      LoadDicts();

      //string ip = "192.168.9.11";  // mes8a
      string ip = "192.168.3.27";  // mes8n
      TextBoxHydraHost.Text = padIP(ip);

      db = new DataClasses1DataContext();
      db2 = new DataClasses2DataContext();

      _serviceTimerInterval = GetServiceTimerInterval();
      _serviceLastProcId = GetServiceLastProcId();
    }

    private long _serviceTicks;

    private int GetServiceTimerInterval()
    {
      int serviceTimerInterval = 0;
      try
      {
        IEnumerable<SvcParam> ti = from param in db.SvcParams where param.Name.ToLower() == "process.timer_interval" select param;
        SvcParam svcParam = ti.First();
        int.TryParse(svcParam.Value, out serviceTimerInterval);
      }
      catch
      {
        serviceTimerInterval = 0;
      }
      return serviceTimerInterval;
    }
    private int GetServiceLastProcId()
    {
      int serviceLastId = 0;
      try
      {
        serviceLastId = db.m_statuses.Max(u => (int)u.id);
      }
      catch
      {
      }
      return serviceLastId;
    }
    private void Service_IntervalInit()
    {
      _serviceTicks = DateTime.Now.Ticks;
    }
    private bool Service_IsIntervalExpired()
    {
      TimeSpan timeSpan = TimeSpan.FromTicks(DateTime.Now.Ticks - _serviceTicks);
      int seconds = timeSpan.Seconds;
      return seconds >= _serviceTimerInterval;
    }
    private void WDisp_Service_MapNextResults()
    {
      //db.Refresh(System.Data.Linq.RefreshMode.KeepChanges, db.m_statuses);
      // кэширование покроем пересозданием контекста
      db.Dispose();
      db = new DataClasses1DataContext();
      IEnumerable<m_statuse> states = from s in db.m_statuses where s.id > _serviceLastProcId && s.processed != (int)StatusProcessingType.NotProcessed select s;
      if (states.Count() == 0)
        return;
      Hashtable mh = new Hashtable();
      Hashtable mh2 = new Hashtable();
      Hashtable mh3 = new Hashtable();
      foreach (m_statuse s in states)
      {
        if (mh.Contains(s.machine_id))
        {
          mh[s.machine_id] = (int)s.processed;
          mh2[s.machine_id] = s.error_msg;
          mh3[s.machine_id] = s.id;
        }
        else
        {
          mh.Add(s.machine_id, (int)s.processed);
          mh2.Add(s.machine_id, s.error_msg);
          mh3.Add(s.machine_id, s.id);

          _serviceLastProcId = s.id;
        }
      }

      this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate()
      {
        for (int i = 0; i < mq; i++)
        {
          DataGridMachines.CurrentItem = DataGridMachines.Items[i];
          Machine m = (Machine)DataGridMachines.CurrentItem;
          if (mh.Contains(m.Mnr))
          {
            m.ProcCode = (int)mh[m.Mnr];
            m.ProcMessage = (string)mh2[m.Mnr];
            m.ProcId = (int)mh3[m.Mnr];
          }

          var row = DataGridMachines.ItemContainerGenerator.ContainerFromItem(m) as DataGridRow;
          if (row != null)
          {
            if (m.ProcCode == (int)StatusProcessingType.ErrorOnSend)
            {
              // если возникла ошибка сервиса, то покажем строку красным фоном
              if(!Equals(row.Background, Brushes.Salmon))
                row.Background = Brushes.Salmon;
            }
            else if (Equals(row.Background, Brushes.Salmon))
            {
              // если ошибки нет, а строка была в красном фоне, то снимем этот красный фон (ошибка исчезла)
              row.Background = Brushes.White;
            }
          }
        }
      });

    }
    private void WDisp_Service_ResetErrorBackColor()
    {
      this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate()
      {
        for (int i = 0; i < mq; i++)
        {
          DataGridMachines.CurrentItem = DataGridMachines.Items[i];
          Machine m = (Machine)DataGridMachines.CurrentItem;
          var row = DataGridMachines.ItemContainerGenerator.ContainerFromItem(m) as DataGridRow;
          if (row != null && Equals(row.Background, Brushes.Salmon))
          {
            // если строка была подсвечена красным как ошибка, то снимем этот красный фон
            row.Background = Brushes.White;
          }
        }
      });
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
      LoadMachines_xml();
      LoadStatesSequence_xml();
      LoadStatesDictionary_xml();
    }

    private bool LoadStatesDictionary_xml()
    {
      XmlReader xmlReader;
      XElement root;
      int i;

      // Заполним словарь состояний (StatesHashtable)
      string xmlPath = AppDomain.CurrentDomain.BaseDirectory + "Custom\\States.xml";
      if (!File.Exists(xmlPath))
      {
        MessageBox.Show("Файл " + xmlPath + " не существует!", "Файл не найден");
        return false;
      }

      try
      {
        xmlReader = XmlReader.Create(xmlPath);
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
      }
      catch (Exception ex)
      {
        MessageBox.Show("Ошибка при разборе xml-файла " + xmlPath + "! " + ex.Message, "Файл имеет некорректную структуру. ");
        return false;
      }

      // Пропишем названия статусов в таблице DataGridStates
      for (int j = 0; j < DataGridStates.Items.Count; j++)
      {
        DataGridStates.CurrentItem = DataGridStates.Items[j];
        State s = (State)DataGridStates.CurrentItem;
        s.Designation = StatesHashtable.Contains(s.Status) ? StatesHashtable[s.Status].ToString() : s.Status.ToString();
      }

      return true;
    }
    private bool LoadStatesDictionary_db()
    {
      Mouse.OverrideCursor = Cursors.Wait;
      try
      {
        Table<stoertexte> stoertexte = db2.GetTable<stoertexte>();
        StatesHashtable.Clear();
        foreach (stoertexte st in stoertexte)
        {
          StatesHashtable.Add((int)st.stoertxt_nr, st.stoer_text_18);
        }

        // Пропишем названия статусов в таблице DataGridStates
        for (int j = 0; j < DataGridStates.Items.Count; j++)
        {
          DataGridStates.CurrentItem = DataGridStates.Items[j];
          State s = (State)DataGridStates.CurrentItem;
          s.Designation = StatesHashtable.Contains(s.Status) ? StatesHashtable[s.Status].ToString() : s.Status.ToString();
        }
      }
      catch (Exception ex)
      {
        Mouse.OverrideCursor = null;
        MessageBox.Show("Ошибка при загрузке словаря статусов из БД. Проверьте в файле app.config настройку WPF_AIPStressTesting01.Properties.Settings.hydra1ConnectionString! " + ex.Message, "Строка подключения к БД.");
        return false;
      }
      finally
      {
        Mouse.OverrideCursor = null;
      }

      return true;
    }
    private bool LoadStatesSequence_xml()
    {
      XmlReader xmlReader;
      XElement root;
      int i, i2;

      string xmlPath = AppDomain.CurrentDomain.BaseDirectory + "Custom\\StatesSequence.xml";
      if (!File.Exists(xmlPath))
      {
        MessageBox.Show("Файл " + xmlPath + " не существует!", "Файл не найден");
        return false;
      }

      try
      {
        // Популируем таблицу DataGridStates
        // DataGridStates.ItemsSource = State.GetStates();
        xmlReader = XmlReader.Create(xmlPath);
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
      }
      catch (Exception ex)
      {
        MessageBox.Show("Ошибка при разборе xml-файла " + xmlPath + "! " + ex.Message, "Файл имеет некорректную структуру. ");
        return false;
      }

      return true;
    }
    private bool LoadMachines_xml()
    {
      // Популируем таблицу DataGridMachines

      XmlReader xmlReader;
      XElement root;

      //DataGridMachines.ItemsSource = Machine.GetMachines();
      string xmlPath = AppDomain.CurrentDomain.BaseDirectory + "Custom\\Machines.xml";
      if (!File.Exists(xmlPath))
      {
        MessageBox.Show("Файл " + xmlPath + " не существует!", "Файл не найден");
        return false;
      }
      try
      {
        xmlReader = XmlReader.Create(xmlPath);
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
      }
      catch (Exception ex)
      {
        MessageBox.Show("Ошибка при разборе xml-файла " + xmlPath + "! " + ex.Message, "Файл имеет некорректную структуру. ");
        return false;
      }

      MachineQuantityMax = DataGridMachines.Items.Count;      // переопределяется по фактическому наличию

      return true;
    }
    private bool LoadMachines_db()
    {
      Mouse.OverrideCursor = Cursors.Wait;
      try
      {
        // загружаем список машин из БД
        this.Cursor = Cursors.Wait;
        Table<maschinen> maschinen = db2.GetTable<maschinen>();
        var ocMachines = new ObservableCollection<Machine>();
        foreach (maschinen m in maschinen)
        {
          ocMachines.Add(new Machine() { Mnr = m.masch_nr, Name = m.bez_lang_18 });
        }
        DataGridMachines.ItemsSource = ocMachines;

        MachineQuantityMax = DataGridMachines.Items.Count;      // переопределяется по фактическому наличию
      }
      catch (Exception ex)
      {
        Mouse.OverrideCursor = null;
        MessageBox.Show("Ошибка при загрузке словаря станков из БД. Проверьте в файле app.config настройку WPF_AIPStressTesting01.Properties.Settings.hydra1ConnectionString! " + ex.Message, "Строка подключения к БД.");
        return false;
      }
      finally
      {
        Mouse.OverrideCursor = null;
      }

      return true;
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

    // ButtonSpinner SpinnerPrognameDelay
    private void SpinnerPrognameDelay_Spin(object sender, SpinEventArgs e)
    {
      var spinner = (ButtonSpinner)sender;
      var txtBox = (TextBox)spinner.Content;
      int i;
      int value = String.IsNullOrEmpty(txtBox.Text) || !int.TryParse(txtBox.Text, out i) ? 0 : Convert.ToInt32(txtBox.Text);

      if (e.Direction == SpinDirection.Increase)
        value++;
      else
        value--;

      if (value < 0)
        value = 0;
      else if (value > PrognameDelayMax)
        value = PrognameDelayMax;

      txtBox.Text = value.ToString();
    }
    private void SpinnerPrognameDelay_KeyDown(object sender, KeyEventArgs e)
    {
      const string rStr = "^(D|NumPad)[0-9]$";

      if (e.Key == Key.Tab)
        return;

      var key = e.Key.ToString();
      var r = new Regex(rStr, RegexOptions.IgnoreCase);
      e.Handled = !r.IsMatch(key);
    }
    private void SpinnerPrognameDelay_KeyUp(object sender, KeyEventArgs e)
    {
      var spinner = (ButtonSpinner)sender;
      var txtBox = (TextBox)spinner.Content;

      if (String.IsNullOrEmpty(txtBox.Text))
        return;

      int i;
      var value = !int.TryParse(txtBox.Text, out i) ? PrognameDelayMax : i;

      if (value == 0 || value == PrognameDelayMax)
      {
        txtBox.Text = value.ToString();
        return;
      }

      if (value < 0 || value > PrognameDelayMax)
      {
        if (value < 0)
          value = 0;
        else if (value > PrognameDelayMax)
          value = PrognameDelayMax;
        txtBox.Text = value.ToString();
      }
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

      if(Equals(e.Row.Background, Brushes.LightSkyBlue))
        e.Row.Background = Brushes.White;
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

      WDisp_Service_ResetErrorBackColor();
      
      Service_IntervalInit();
      _serviceLastProcId = GetServiceLastProcId();


      DataGridMachines.UnselectAll();

      _thread = new Thread(Test);
      _threadStarted = true;
      _thread.Start();
      ButtonStart.IsEnabled = false;
      ButtonStop.IsEnabled = true;
    }

    private volatile bool _buttonStopProcessed;
    private void ButtonStop_Click(object sender, RoutedEventArgs e)
    {
      _buttonStopProcessed = false;

      Cursor cursor = this.Cursor;
      Mouse.OverrideCursor = Cursors.Wait;

      try
      {
        //Cursor cursor = this.Cursor;
        this.Cursor = Cursors.Wait;
        _threadStarted = false;
        ButtonStop.IsEnabled = false;
        _NestedEmptyMessageLoop();
        while (_thread != null && _thread.Join(2000)) { _thread.Interrupt(); _thread = null; }
        ButtonStart.IsEnabled = true;
        //this.Cursor = cursor;
      }
      catch
      {
      }
      finally
      {
        Mouse.OverrideCursor = null;
      }

      _buttonStopProcessed = true;

      Mouse.OverrideCursor = null;
      this.Cursor = cursor;
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
        WDisp_RestoreMoreNotUsedMachines();
        mq = _MachineQuantity();
        tsf = _TimeScaleFactor();
      });
    }

    private void WDisp_GetMachineQuantity()
    {
      this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate()
      {
        WDisp_RestoreMoreNotUsedMachines();
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

    private void WDisp_RestoreMoreNotUsedMachines()
    {
      this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate()
      {
        _machineQuantityNew = _MachineQuantity();
        if (_machineQuantityNew < mq)
        {
          // очистим состояние записей таблицы DataGridMachines за пределами нового количества тестируемых станков (_machineQuantityNew)
          for (int i = _machineQuantityNew; i < mq; i++)
          {
            DataGridMachines.CurrentItem = DataGridMachines.Items[i];
            Machine m = (Machine)DataGridMachines.CurrentItem;
            m.MsDelay = 0;
            m.MsInStatus = 0;
            m.SeqStep = 0;
            m.Status = 0;
            m.Designation = "";

            // если цвет фона изменённый (меняется при смене статуса или при ошибке сервиса) - восстанавливаем
            var row = DataGridMachines.ItemContainerGenerator.ContainerFromItem(m) as DataGridRow;
            //if ((row != null) && Equals(row.Background, Brushes.LightSkyBlue))
            if ((row != null) && !Equals(row.Background, Brushes.White))
            {
              row.Background = Brushes.White;
            }
          }
        }
      });
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

    private volatile bool _IsCheckSupressDBactionsChecked;
    private bool WDisp_IsCheckSupressDBactionsChecked()
    {
      this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate()
      {
        _IsCheckSupressDBactionsChecked = CheckSupressDBactions.IsChecked.Value;
      });
      return _IsCheckSupressDBactionsChecked;
    }

    private void WDisp_gridSetMachineStatus(int rowIdx, State state, int rowStateGridIdx)
    {
      this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate()
        {
          //DataGridMachines.SelectedItem = DataGridMachines.Items[rowIdx];
          //Machine m = (Machine)DataGridMachines.SelectedItem;
          // DataGridMachines.Focus();

          DataGridMachines.CurrentItem = DataGridMachines.Items[rowIdx];
          Machine m = (Machine)DataGridMachines.CurrentItem;

          m.SeqStep = rowStateGridIdx + 1;
          m.Status = state.Status;
          m.Designation = StatesHashtable.Contains(state.Status) ? StatesHashtable[state.Status].ToString() : m.Status.ToString();
          m.MsDelay = state.MsDelay;
          m.MsInStatus = 0;
          // на мин.интервал времени меняем цвет фона (чтобы показать, что изменился статус)
          // и делаем это только для видимых записей, не имеющих кода ошибки после обработки сервисом!
          ScrollViewer scrollviewDataGridMachines = FindVisualChild<ScrollViewer>(DataGridMachines);
          int visTopRowIdx = (int) scrollviewDataGridMachines.ContentVerticalOffset;
          int visBotRowIdx = (int) (visTopRowIdx + scrollviewDataGridMachines.ViewportHeight + 1);
          if (rowIdx >= visTopRowIdx && rowIdx <= visBotRowIdx)
          {
            var row = DataGridMachines.ItemContainerGenerator.ContainerFromItem(m) as DataGridRow;
            if (row != null && !Equals(row.Background, Brushes.Salmon))
              row.Background = Brushes.LightSkyBlue;
          }
        });
    }

    public static T FindVisualChild<T>(DependencyObject depObj) where T : DependencyObject
    {
      if (depObj != null)
      {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
        {
          DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
          if (child != null && child is T)
          {
            return (T)child;
          }

          T childItem = FindVisualChild<T>(child);
          if (childItem != null) return childItem;
        }
      }
      return null;
    }

    private volatile string _mnr;
    private string WDisp_gridGetMachineId(int rowIdx)
    {
      this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate()
      {
        DataGridMachines.CurrentItem = DataGridMachines.Items[rowIdx];
        Machine m = (Machine)DataGridMachines.CurrentItem;
        _mnr = m.Mnr;
      });
      return _mnr;
    }

    private volatile int _state;
    private int WDisp_gridGetStateId(int rowIdx)
    {
      this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate()
      {
        DataGridStates.CurrentItem = DataGridStates.Items[rowIdx];
        State s = (State)DataGridStates.CurrentItem;
        _state = s.Status;
      });
      return _state;
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

          // если цвет фона изменённый (меняется при смене статуса) - восстанавливаем через мин.интервал времени
          var row = DataGridMachines.ItemContainerGenerator.ContainerFromItem(m) as DataGridRow;
          if ((row != null) && Equals(row.Background, Brushes.LightSkyBlue))
          {
            //if (i%2 == 0)
            //  row.Background = Brushes.White;
            //else
            //  row.Background = DataGridMachines.AlternatingRowBackground;
            row.Background = Brushes.White;
          }

        }
      });
    }

    private void WDisp_MessageBox(string text, string title)
    {
      this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate()
      {
        MessageBox.Show(text, title);
      });
    }
    private void WDisp_ButtonStop_Click()
    {
      this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate()
      {
        ButtonStop_Click(null, null);
      });
    }

    private long make3(int machineIdx, int stateIdx, int msDelay)
    {
      //return machineIdx + 1000 * stateIdx + (long)1000000 * msDelay;
      long l = (((long)msDelay) << 22) + ((long)stateIdx << 9) + machineIdx;
      return l;
    }
    private int MachineIdxFrom3(long l3)
    {
      //return (int)(l3 % 1000);
      int i = (int) (l3 & 0x1FF);
      return i;
    }
    private int StateIdxFrom3(long l3)
    {
      //return (int)((l3 % 1000000) / 1000);
      int i = (int) ((l3 >> 9) & 0x1FFF);
      return i;
    }
    private int MsDelayFrom3(long l3)
    {
      //return (int)(l3 / 1000000);
      int i = (int)((l3 >> 22) & 0x000003FFFFFFFFFF);
      return i;
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
          WDisp_gridSetMachineStatus(i, (State)stateItem, j);   // в таблице установим новый статус
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
            //WDisp_gridForAllMachinesAddToMsInStatus((int)(MaxDelayForOneStep * 1000 / tsf));
            WDisp_gridForAllMachinesAddToMsInStatus((int)(ssToDelayStep * 1000 / tsf));
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

              // шлём команду на сервер (= добавление записи в таблицу БД dbo.m_statuses)
              if (!WDisp_IsCheckSupressDBactionsChecked())
              {
                try
                {
                  m_statuse mst = new m_statuse();
                  mst.id = 0;
                  mst.machine_id = WDisp_gridGetMachineId(m1);
                  mst.status = WDisp_gridGetStateId(s1);
                  mst.status_dt = DateTime.Now;
                  db.m_statuses.InsertOnSubmit(mst);
                  db.SubmitChanges();
                }
                catch (Exception ex)
                {
                  WDisp_MessageBox("Ошибка при подключении к БД. Проверьте в файле app.config настройку WPF_AIPStressTesting01.Properties.Settings.hypdmConnectionString! " + ex.Message, "Строка подключения к БД.");
                  if (_threadStarted)
                    WDisp_ButtonStop_Click();
                  break;
                }
              }
              // прописываем следующее состояние
              s1++;
              if (s1 >= sq)
                s1 = 0;
              var stateItem = (State)stateItemCollection[s1];
              stateIdx[i] = make3(m1, s1, stateItem.MsDelay);
              WDisp_gridSetMachineStatus(m1, (State)stateItem, s1);   // в таблице установим новый статус
            }
            else
              break;
          }

          if (!_threadStarted) break;
          if (Service_IsIntervalExpired())
          {
            Service_IntervalInit();
            if (!WDisp_IsCheckSupressDBactionsChecked())
              WDisp_Service_MapNextResults();
          }

          Array.Sort(stateIdx);
        }
        if (!_threadStarted) break;

      }
    }

    private void DataGridMachines_AutoGeneratedColumns(object sender, EventArgs e)
    {
      DataGridMachines.FrozenColumnCount = 2;

      //DataGridMachines.Columns[1].Width = 70; // DataGridLength.SizeToHeader
      //DataGridMachines.Columns[3].Width = DataGridLength.SizeToHeader;
      //DataGridMachines.Columns[4].Width = 80;
      //DataGridMachines.Columns[8].Width = 260;

      Style styleAlignmentRight = new Style();
      styleAlignmentRight.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));

      foreach (DataGridTextColumn col in DataGridMachines.Columns)
      {
        switch ((string)col.SortMemberPath)
        {
          case "Name":
            col.Width = 55;   // DataGridLength.SizeToHeader
            break;
          case "SeqStep":
            col.CellStyle = styleAlignmentRight;
            break;
          case "Status":
            col.Width = DataGridLength.SizeToHeader;
            col.CellStyle = styleAlignmentRight;
            break;
          case "Designation":
            col.Width = 70;
            break;
          case "MsDelay":
            col.CellStyle = styleAlignmentRight;
            break;
          case "MsInStatus":
            col.CellStyle = styleAlignmentRight;
            break;
          case "ProcCode":
            col.CellStyle = styleAlignmentRight;
            break;
          case "ProcMessage":
            col.Width = 260;
            break;
          case "ProcId":
            col.CellStyle = styleAlignmentRight;
            break;
        }
      }

    }

    private void DataGridStates_AutoGeneratedColumns(object sender, EventArgs e)
    {
      //DataGridStates.Columns[1].Width = 120; // DataGridLength.SizeToHeader;

      Style styleAlignmentRight = new Style();
      styleAlignmentRight.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));

      foreach (DataGridTextColumn col in DataGridStates.Columns)
      {
        switch ((string) col.SortMemberPath)
        {
          case "Status":
            col.CellStyle = styleAlignmentRight;
            break;
          case "Designation":
            col.Width = 120; // DataGridLength.SizeToHeader;
            break;
          case "MsDelay":
            col.CellStyle = styleAlignmentRight;
            break;
        }
      }
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

    private void CheckLoadMachinesFromDb_Click(object sender, RoutedEventArgs e)
    {
      Cursor cursor = this.Cursor;
      if (_threadStarted)
      {
        mq = 1;
        ButtonStop_Click(null, null);
        while (!_buttonStopProcessed)
        {
          // ждём окончания обработки, вызванной по кнопке "Stop"
          Thread.Sleep(TimeSpan.FromSeconds(.1));
        }
      }
      var chk = (CheckBox)sender;
      if (chk.IsChecked.Value)
      {
        // загружаем список машин из БД
        if (!LoadMachines_db())
          CheckLoadMachinesFromDb.IsChecked = false;
      }
      else
      {
        // загружаем список машин из Machines.xml
        if (!LoadMachines_xml())
          CheckLoadMachinesFromDb.IsChecked = true;
      }
      var txtBox = (TextBox)SpinnerMachineQuantity.Content;
      int i;
      int value = String.IsNullOrEmpty(txtBox.Text) || !int.TryParse(txtBox.Text, out i) ? 0 : Convert.ToInt32(txtBox.Text);
      if (value > MachineQuantityMax)
        txtBox.Text = MachineQuantityMax.ToString();
      this.Cursor = cursor;
    }

    private void CheckLoadStatesFromDb_Click(object sender, RoutedEventArgs e)
    {
      Cursor cursor = this.Cursor;
      if (_threadStarted)
      {
        mq = 1;
        ButtonStop_Click(null, null);
        while (!_buttonStopProcessed)
        {
          // ждём окончания обработки, вызванной по кнопке "Stop"
        }
      }
      var chk = (CheckBox)sender;
      if (chk.IsChecked.Value)
      {
        if (!LoadStatesDictionary_db())
          CheckLoadStatesFromDb.IsChecked = false;
      }
      else
      {
        if (!LoadStatesDictionary_xml())
          CheckLoadStatesFromDb.IsChecked = true;
      }
      this.Cursor = cursor;
    }

    private void DataGridMachines_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (_threadStarted)
      {
        // пока находимся в режиме стартовавшего тестирования, выбор в таблице станков запрещаем
        if (dgmMouseOrKeyboardEventOnRow(e))
          e.Handled = true;
      }
    }

    private void DataGridMachines_PreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
      if (_threadStarted)
      {
        if (dgmMouseOrKeyboardEventOnRow(e))
          e.Handled = true;
      }
    }

    private bool dgmMouseOrKeyboardEventOnRow(RoutedEventArgs e)
    {
      DependencyObject dep = (DependencyObject)e.OriginalSource;
      while ((dep != null) && !(dep is DataGridCell) && !(dep is DataGridColumnHeader) && !(dep is DataGridRowHeader))
      {
        dep = VisualTreeHelper.GetParent(dep);
      }
      return (dep != null);
    }

    private void DataGridStates_Sorting(object sender, DataGridSortingEventArgs e)
    {
      e.Handled = true;
    }

    private void CheckSupressDBactions_Checked(object sender, RoutedEventArgs e)
    {
      if (_threadStarted)
        WDisp_Service_ResetErrorBackColor();
    }

  }
}
