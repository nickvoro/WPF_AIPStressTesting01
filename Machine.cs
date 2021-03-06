﻿using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace WPF_AIPStressTesting01
{
  public class Machine : INotifyPropertyChanged
  {
    private string _mnr;
    public string Mnr
    {
      get { return _mnr; }
      set
      {
        if (value == _mnr) return;
        _mnr = value;
        OnPropertyChanged("Mnr");
      }
    }

    private string _name;
    public string Name
    {
      get { return _name; }
      set
      {
        if (value == _name) return;
        _name = value;
        OnPropertyChanged("Name");
      }
    }

    private int _seqStep;
    public int SeqStep
    {
      get { return _seqStep; }
      set
      {
        if (value == _seqStep) return;
        _seqStep = value;
        OnPropertyChanged("SeqStep");
      }
    }

    private int _status;
    public int Status
    {
      get { return _status; }
      set
      {
        if (value == _status) return;
        _status = value;
        OnPropertyChanged("Status");
      }
    }

    private string _designation;
    public string Designation
    {
        get { return _designation; }
        set
        {
            if (value == _designation) return;
            _designation = value;
            OnPropertyChanged("Designation");
        }
    }

    private int _ms_delay;
    public int MsDelay
    {
      get { return _ms_delay; }
      set
      {
        if (value == _ms_delay) return;
        _ms_delay = value; ;
        OnPropertyChanged("MsDelay");
      }
    }

    private int _ms_in_status;

    public int MsInStatus
    {
      get { return _ms_in_status; }
      set
      {
        if (value == _ms_in_status) return;
        _ms_in_status = value;
        ;
        OnPropertyChanged("MsInStatus");
      }
    }

    private int _proc_code;
    public int ProcCode
    {
      get { return _proc_code; }
      set
      {
        if (value == _proc_code) return;
        _proc_code = value;
        ;
        OnPropertyChanged("ProcCode");
      }
    }

    private string _proc_message;
    public string ProcMessage
    {
        get { return _proc_message; }
        set
        {
            if (value == _proc_message) return;
            _proc_message = value;
            OnPropertyChanged("ProcMessage");
        }
    }

    private int _proc_id;
    public int ProcId
    {
      get { return _proc_id; }
      set
      {
        if (value == _proc_id) return;
        _proc_id = value;
        ;
        OnPropertyChanged("ProcId");
      }
    }

/*
    public static ObservableCollection<Machine> GetMachines()
    {
      var machines = new ObservableCollection<Machine>();
      machines.Add(new Machine() { Mnr = "18210004", Status = 1 });
      machines.Add(new Machine() { Mnr = "18210005", Status = 1 });
      machines.Add(new Machine() { Mnr = "18210009", Status = 4 });
      machines.Add(new Machine() { Mnr = "18210012", Status = 3 });
      machines.Add(new Machine() { Mnr = "18210018", Status = 3 });
      machines.Add(new Machine() { Mnr = "18210026", Status = 3 });
      machines.Add(new Machine() { Mnr = "18290017", Status = 3 });
      machines.Add(new Machine() { Mnr = "18290025", Status = 3 });
      machines.Add(new Machine() { Mnr = "18290032", Status = 1 });
      machines.Add(new Machine() { Mnr = "18290034", Status = 3 });
      machines.Add(new Machine() { Mnr = "16290048", Status = 1 });
      machines.Add(new Machine() { Mnr = "16290076", Status = 5 });
      return machines;
    }
*/
    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChangedEventHandler handler = PropertyChanged;
      if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}