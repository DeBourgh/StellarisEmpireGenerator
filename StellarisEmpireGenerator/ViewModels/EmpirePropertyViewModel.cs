using StellarisEmpireGenerator.Core;
using StellarisEmpireGenerator.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace StellarisEmpireGenerator.ViewModels
{
	public class EmpirePropertyViewModel : ObservableObject
	{
		public EmpirePropertyViewModel() { }
		public EmpirePropertyViewModel(EmpireProperty Property)
		{
			Source = Property;
			_weight = Source.Weight;
		}

		public EmpireProperty Source { get; private set; }

		private bool _isSelected = false;
		public bool IsSelected
		{
			get => _isSelected;
			set { SetProperty(ref _isSelected, value); }
		}

		public string Identifier { get => Source.Identifier; }
		//private string _name;
		public string Name { get => Source.Name; }

		private int _weight;
		public int Weight
		{
			get => _weight;
			set
			{
				SetProperty(ref _weight, value);
				Source.Weight = _weight;
			}
		}

		private bool _isAllowed = true;
		public bool IsAllowed
		{
			get => _isAllowed;
			set { SetProperty(ref _isAllowed, value); }
		}

		private bool _isRestricted = false;
		public bool IsRestricted
		{
			get => _isRestricted;
			set { SetProperty(ref _isRestricted, value); }
		}

		private ICommand _switchIsAllowedCommand;
		public ICommand SwitchIsAllowedCommand
		{
			get
			{
				if (_switchIsAllowedCommand == null)
				{
					_switchIsAllowedCommand = new RelayCommand(
						p => true,
						p => IsAllowed = !IsAllowed);
				}

				return _switchIsAllowedCommand;
			}
		}

		public override string ToString()
		{
			return Source?.Identifier ?? base.ToString();
		}

		public override int GetHashCode()
		{
			return Source.GetHashCode();
		}
	}
}
