using System.Windows.Controls;

namespace InterdisciplinairProject.Views
{
    public partial class SceneDurationControl : UserControl
    {
        public SceneDurationControl()
        {
            InitializeComponent();
            InfoButton.Click += (s, e) => DurationPopup.IsOpen = !DurationPopup.IsOpen;
        }
    }
}
