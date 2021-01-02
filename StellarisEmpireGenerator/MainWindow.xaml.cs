using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using StellarisEmpireGenerator.ViewModels;

namespace StellarisEmpireGenerator
{
	/// <summary>
	/// Interaktionslogik für MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private MainViewModel ViewModel { get { return DataContext as MainViewModel; } }

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			ViewModel.SaveConfigToFile();
		}
	}
}
