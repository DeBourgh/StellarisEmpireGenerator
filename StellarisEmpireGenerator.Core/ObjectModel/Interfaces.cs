using System;
using System.Collections.Generic;
using System.Text;

namespace StellarisEmpireGenerator.Core.ObjectModel
{
	public interface IEntityValue
	{
		object Value { get; set; }
	}


	public interface IEntityValue<T> : IEntityValue
	{
		new T Value { get; set; }
	}


}
