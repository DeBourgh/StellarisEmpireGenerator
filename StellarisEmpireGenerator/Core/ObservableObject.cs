using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace StellarisEmpireGenerator.Core
{
	/// <summary>
	/// A base for objects using property notification.
	/// Source: https://github.com/Tosker/ValidationInWPF/blob/master/DataValidation/B_Validation_ByDataErrorInfo/RegistrationVM.cs
	/// </summary>
	public class ObservableObject : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Notify a property change
		/// </summary>
		/// <param name="PropertyName">Name of property to update</param>
		protected virtual void OnPropertyChanged(string PropertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
		}

		/// <summary>
		/// Notify a property change that uses CallerMemberName attribute
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="Property">Backing field of property</param>
		/// <param name="PropertyValue">Value to give backing field</param>
		/// <param name="PropertyName"></param>
		/// <returns></returns>
		protected virtual bool SetProperty<T>(ref T Property, T PropertyValue, [CallerMemberName] string PropertyName = "")
		{
			if (EqualityComparer<T>.Default.Equals(Property, PropertyValue))
				return false;

			Property = PropertyValue;
			OnPropertyChanged(PropertyName);
			return true;
		}
	}
}
