using InterdisciplinairProject.ViewModels;
using System.Windows;
using SceneModel = InterdisciplinairProject.Core.Models.Scene;

namespace InterdisciplinairProject.Views;

/// <summary>
/// Interaction logic for SceneSettingsWindow.xaml.
/// </summary>
public partial class SceneSettingsWindow : Window
{
    public SceneSettingsWindow(SceneModel scene)
    {
        InitializeComponent();
        DataContext = new SceneSettingsViewModel(this, scene);
    }
}
