using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.ViewManagement;
using Windows.ApplicationModel.Core;
using UltraTextEdit_UWP.Views.Settings;
using System;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UltraTextEdit_UWP.Dialogs
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FirstRunDialog : ContentDialog
    {
        public FirstRunDialog()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Hides default title bar and replaces with custom title bar 
        /// </summary>
        /// <param name="sender">Grid</param>
        /// <param name="e">Loaded</param>

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender">Button</param>
        /// <param name="e">Click</param>

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButtonWhatsNew_Click(object sender, ContentDialogButtonClickEventArgs e)
        {
            this.Hide();
            await new WhatsNewDialog().ShowAsync();
        }

        private void ButtonStartUsing_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            this.Hide();
        }
    }
}