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
      double d  ;
      var value = String.IsNullOrEmpty(txtBox.Text) || !double.TryParse(txtBox.Text, out d) ? 0 : Convert.ToDouble(txtBox.Text);
      double delta ;

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

      if (value == 0 || value == TimeScaleFactorMax)
      {
        txtBox.Text = value.ToString();
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
      if ((_thread != null)) while (_thread.Join(1000)) {};
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

    private void Test()
    {
        while (_threadStarted)
        {
          Thread.Sleep(TimeSpan.FromSeconds(4));
          Thread.Sleep(TimeSpan.FromSeconds(4));
        }
    }
  
  }
}
