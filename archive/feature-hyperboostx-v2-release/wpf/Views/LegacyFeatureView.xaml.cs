using System.Windows.Controls;
using HyperBoostX.ViewModels;

namespace HyperBoostX.Views
{
    public partial class LegacyFeatureView : UserControl
    {
        public LegacyFeatureView()
        {
            InitializeComponent();
        }

        public LegacyFeatureView(LegacyFeaturePageViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}
