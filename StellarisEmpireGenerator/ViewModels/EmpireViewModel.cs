using StellarisEmpireGenerator.Core;
using StellarisEmpireGenerator.Core.EmpireProperties;

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

		public EmpireViewModel(
			EmpireProperty Authority,
			IEnumerable<EmpireProperty> Civics,
			IEnumerable<EmpireProperty> Ethics,
			EmpireProperty Origin,
			EmpireProperty Species,
			IEnumerable<EmpireProperty> Traits)
		{
			this.Authority = Authority;
			this.Civics = Civics;
			this.Ethics = Ethics;
			this.Origin = Origin;
			this.Species = Species;
			this.Traits = Traits;
		}

		public EmpireProperty Authority { get; private set; }
		public IEnumerable<EmpireProperty> Civics { get; private set; }
		public IEnumerable<EmpireProperty> Ethics { get; private set; }
		public EmpireProperty Origin { get; private set; }
		public EmpireProperty Species { get; private set; }
		public IEnumerable<EmpireProperty> Traits { get; private set; }


		private string _title = "Empire #";
		public string Title
		{
			get => _title;
			set { SetProperty(ref _title, value); }
		}

		private string _authorityText;
		public string AuthorityText
		{
			get => _authorityText;
			set { SetProperty(ref _authorityText, value); }
		}

		public ObservableCollection<string> CivicsTexts { get; } = new ObservableCollection<string>();

		public ObservableCollection<string> EthicsTexts { get; } = new ObservableCollection<string>();

		private string _originText;
		public string OriginText
		{
			get => _originText;
			set { SetProperty(ref _originText, value); }
		}

		private string _speciesText;
		public string SpeciesText
		{
			get => _speciesText;
			set { SetProperty(ref _speciesText, value); }
		}

		public ObservableCollection<string> TraitsTexts { get; } = new ObservableCollection<string>();

		public void ApplyLanguageDict(IDictionary<string, string> Dict)
		{
			string value;

			if (Authority != null)
			{
				string authId = Authority.Identifier;

				if (Dict != null && Dict.TryGetValue(authId, out value))
					AuthorityText = value;
				else
					AuthorityText = authId;
			}

			if (Civics != null)
			{
				foreach (var civic in Civics)
				{
					if (civic != null)
					{
						string civicId = civic.Identifier;

						if (Dict != null && Dict.TryGetValue(civicId, out value))
							CivicsTexts.Add(value);
						else
							CivicsTexts.Add(civicId);
					}
				}
			}

			if (Ethics != null)
			{
				foreach (var ethic in Ethics)
				{
					if (ethic != null)
					{
						string ethicId = ethic.Identifier;

						if (Dict != null && Dict.TryGetValue(ethicId, out value))
							EthicsTexts.Add(value);
						else
							EthicsTexts.Add(ethicId);
					}
				}
			}

			if (Origin != null)
			{
				string originId = Origin.Identifier;

				if (Dict != null && Dict.TryGetValue(originId, out value))
					OriginText = value;
				else
					OriginText = originId;
			}

			if (Species != null)
			{
				string speciesId = Species.Identifier;

				if (Dict != null && Dict.TryGetValue(speciesId, out value))
					SpeciesText = value;
				else
					SpeciesText = speciesId;
			}

			if (Traits != null)
			{
				foreach (var traits in Traits)
				{
					if (traits != null)
					{
						string traitId = traits.Identifier;

						if (Dict != null && Dict.TryGetValue(traitId, out value))
							TraitsTexts.Add(value);
						else
							TraitsTexts.Add(traitId);
					}
				}
			}
		}
	}
}
