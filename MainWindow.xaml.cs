using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit;
using System.Text.RegularExpressions;

namespace WPF_AIPStressTesting01
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      InitializeComponent();
    }

    // ButtonSpinner

    private void ButtonSpinner_Spin(object sender, SpinEventArgs e)
    {
      ButtonSpinner spinner = (ButtonSpinner)sender;
      TextBox txtBox = (TextBox)spinner.Content;
      int i;
      int value = String.IsNullOrEmpty(txtBox.Text) || !int.TryParse(txtBox.Text, out i) ? 0 : Convert.ToInt32(txtBox.Text);
      
      if (e.Direction == SpinDirection.Increase)
        value++;
      else
        value--;

      if (value < 1)
        value = 100;
      else if (value > 100)
        value = 1;

      txtBox.Text = value.ToString();
    }

    private void ButtonSpinner_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Tab)
      {
        return;
      }
      
      var key = e.Key.ToString();
      var rStr = "^(D|NumPad)[0-9]$";
      var r = new Regex(rStr, RegexOptions.IgnoreCase);
      e.Handled = !r.IsMatch(key);
    }

    private void ButtonSpinner_KeyUp(object sender, KeyEventArgs e)
    {
      ButtonSpinner spinner = (ButtonSpinner)sender;
      TextBox txtBox = (TextBox)spinner.Content;

      if (String.IsNullOrEmpty(txtBox.Text))
        return;

      int i;
      int value = String.IsNullOrEmpty(txtBox.Text) || !int.TryParse(txtBox.Text, out i) ? 0 : Convert.ToInt32(txtBox.Text);

      if (value == 0)
        return;
      
      if (value < 1 || value > 100)
      {
        if (value < 1)
          value = 1;
        else if (value > 100)
          value = 100;
        txtBox.Text = value.ToString();
      }
    }
  }
}
