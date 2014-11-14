using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace WPF_AIPStressTesting01
{
    class State : INotifyPropertyChanged
    {

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

/*
        public static ObservableCollection<State> GetStates()
        {
            var states = new ObservableCollection<State>();
            states.Add(new State() { Status = 1, MsDelay = 5000 });
            states.Add(new State() { Status = 2, MsDelay = 4000 });
            states.Add(new State() { Status = 5, MsDelay = 3000 });
            states.Add(new State() { Status = 3, MsDelay = 4000 });
            states.Add(new State() { Status = 4, MsDelay = 5000 });
            return states;
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
