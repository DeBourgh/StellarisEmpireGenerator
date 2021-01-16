using StellarisEmpireGenerator.Core;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StellarisEmpireGenerator.ViewModels
{
	public class EmpireViewModel : ObservableObject
	{
		public EmpireViewModel() { }

		private string _title = "Empire #";
		public string Title
		{
			get => _title;
			set { SetProperty(ref _title, value); }
		}

		private string _authority;
		public string Authority
		{
			get => _authority;
			set { SetProperty(ref _authority, value); }
		}

		public ObservableCollection<string> Civics { get; } = new ObservableCollection<string>();

		public ObservableCollection<string> Ethics { get; } = new ObservableCollection<string>();

		private string _origin;
		public string Origin
		{
			get => _origin;
			set { SetProperty(ref _origin, value); }
		}

		private string _species;
		public string Species
		{
			get => _species;
			set { SetProperty(ref _species, value); }
		}

		public ObservableCollection<string> Traits { get; } = new ObservableCollection<string>();
	}
}
