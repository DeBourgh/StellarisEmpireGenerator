using StellarisEmpireGenerator.Core;
using StellarisEmpireGenerator.Core.EmpireProperties;
using StellarisEmpireGenerator.ObjectModel;


using System.Windows.Input;

namespace StellarisEmpireGenerator.ViewModels
{
	public class EmpirePropertyViewModel : ObservableObject
	{
		public EmpirePropertyViewModel() { }
		public EmpirePropertyViewModel(EmpireProperty Property)
		{
			Source = Property;
			//this.Name = Name;
			IsAllowed = Source.IsAllowed;
			Weight = Source.Weight;
		}

		public EmpireProperty Source { get; private set; }

		private string _name = string.Empty;
		public string Name
		{
			get => _name;
			set { SetProperty(ref _name, value); }
		}

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
			set
			{
				SetProperty(ref _isAllowed, value);
				Source.IsAllowed = _isAllowed;
			}
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
