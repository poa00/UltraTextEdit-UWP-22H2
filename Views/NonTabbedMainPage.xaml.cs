﻿using UltraTextEdit_UWP.Helpers;
using UltraTextEdit_UWP.Views;
using UltraTextEdit_UWP.Views.Settings;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Graphics.Printing;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Core.Preview;
using Windows.UI.Text;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using System.Reflection.Metadata;
using System.Text;
using Windows.UI.Xaml.Media.Imaging;
using UltraTextEdit_UWP.Dialogs;
using System.Runtime.CompilerServices;
using MicaForUWP.Media;
using Microsoft.UI.Xaml.Controls;

namespace UltraTextEdit_UWP
{
    public sealed partial class NonTabbedMainPage : Page
    {
        public bool saved = true;
        public bool _wasOpen = false;
        string appTitleStr = Strings.Resources.AppName;
        string fileNameWithPath = "";
        string originalDocText = "";

        public NonTabbedMainPage()
        {
            InitializeComponent();

            if (BuildInfo.BeforeWin11)
            {
                if (App.Current.RequestedTheme == ApplicationTheme.Light)
                {
                    Application.Current.Resources["AppTitleBarBrush"] = new BackdropMicaBrush()
                    {
                        LuminosityOpacity = 0.8F,
                        TintOpacity = 0F,
                        BackgroundSource = BackgroundSource.WallpaperBackdrop,
                        Opacity = 1,
                        TintColor = Windows.UI.Color.FromArgb(255, 230, 230, 230),
                        FallbackColor = Windows.UI.Color.FromArgb(255, 230, 230, 230)
                    };
                    this.Background = (Brush)Application.Current.Resources["AppTitleBarBrush"];
                }
                else
                {
                    Application.Current.Resources["AppTitleBarBrush"] = new BackdropMicaBrush()
                    {
                        LuminosityOpacity = 0.8F,
                        TintOpacity = 0F,
                        BackgroundSource = BackgroundSource.WallpaperBackdrop,
                        Opacity = 1,
                        TintColor = Windows.UI.Color.FromArgb(255, 25, 25, 25),
                        FallbackColor = Windows.UI.Color.FromArgb(25, 25, 25, 25)
                    };
                    this.Background = (Brush)Application.Current.Resources["AppTitleBarBrush"];
                }

            }
            else
            {

            }

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            var appViewTitleBar = ApplicationView.GetForCurrentView().TitleBar;

            appViewTitleBar.ButtonBackgroundColor = Colors.Transparent;
            appViewTitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            appViewTitleBar.ButtonForegroundColor = (Color)Resources["SystemAccentColor"];

            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            UpdateTitleBarLayout(coreTitleBar);

            Window.Current.SetTitleBar(AppTitleBar);

            coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;
            coreTitleBar.IsVisibleChanged += CoreTitleBar_IsVisibleChanged;
            Window.Current.Activated += Current_Activated;

            SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += OnCloseRequest;

            NavigationCacheMode = NavigationCacheMode.Required;

            ShareSourceLoad();

            var LocalSettings = ApplicationData.Current.LocalSettings;
            if (LocalSettings.Values["SpellCheck"] != null)
            {
                if (LocalSettings.Values["SpellCheck"].ToString() == "On")
                {
                    editor.IsSpellCheckEnabled = true;
                }
                else
                {
                    editor.IsSpellCheckEnabled = false;
                }
            }
            else
            {
                LocalSettings.Values["SpellCheck"] = "Off";
            }
            if (LocalSettings.Values["NewFindReplaceVID"] != null)
            {
                if (LocalSettings.Values["NewFindReplaceVID"].ToString() == "On")
                {
                    findreplacepanel.Visibility = Visibility.Visible;
                }
                else
                {
                    findreplacepanel.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                LocalSettings.Values["NewFindReplaceVID"] = "Off";
            }

        }

        private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            UpdateTitleBarLayout(sender);
        }

        private void CoreTitleBar_IsVisibleChanged(CoreApplicationViewTitleBar sender, object args)
        {
            AppTitleBar.Visibility = sender.IsVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        // Update the TitleBar based on the inactive/active state of the app
        private void Current_Activated(object sender, WindowActivatedEventArgs e)
        {
            SolidColorBrush defaultForegroundBrush = new SolidColorBrush((Color)Application.Current.Resources["SystemAccentColor"]);
            SolidColorBrush inactiveForegroundBrush = (SolidColorBrush)Application.Current.Resources["TextFillColorDisabledBrush"];

            if (e.WindowActivationState == CoreWindowActivationState.Deactivated)
            {
                AppTitle.Foreground = inactiveForegroundBrush;
            }
            else
            {
                AppTitle.Foreground = defaultForegroundBrush;
            }
        }

        private void UpdateTitleBarLayout(CoreApplicationViewTitleBar coreTitleBar)
        {
            // Update title bar control size as needed to account for system size changes.
            AppTitleBar.Height = coreTitleBar.Height;

            // Ensure the custom title bar does not overlap window caption controls
            Thickness currMargin = AppTitleBar.Margin;
            AppTitleBar.Margin = new Thickness(currMargin.Left, currMargin.Top, currMargin.Right, currMargin.Bottom);
            TitleBar.Margin = new Thickness(0, currMargin.Top, coreTitleBar.SystemOverlayRightInset, currMargin.Bottom);
        }

        private void OnCloseRequest(object sender, SystemNavigationCloseRequestedPreviewEventArgs e)
        {
            if (!saved) { e.Handled = true; ShowUnsavedDialog(); }
        }

        private void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFile(true);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFile(false);
        }

        public async void SaveFile(bool isCopy)
        {
            string fileName = AppTitle.Text.Replace(" - " + appTitleStr, "");
            if (isCopy || fileName == "Untitled")
            {
                FileSavePicker savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

                // Dropdown of file types the user can save the file as
                savePicker.FileTypeChoices.Add("Rich Text", new List<string>() { ".rtf" });
                savePicker.FileTypeChoices.Add("Plain Text", new List<string>() { ".txt" });

                // Default file name if the user does not type one in or select a file to replace
                savePicker.SuggestedFileName = "New Document";

                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    // Prevent updates to the remote version of the file until we
                    // finish making changes and call CompleteUpdatesAsync.
                    CachedFileManager.DeferUpdates(file);
                    // write to file
                    using (IRandomAccessStream randAccStream = await file.OpenAsync(FileAccessMode.ReadWrite))

                        if (file.Name.EndsWith(".txt"))
                        {
                            editor.Document.SaveToStream(Windows.UI.Text.TextGetOptions.None, randAccStream);
                        }
                        else
                        {
                            editor.Document.SaveToStream(Windows.UI.Text.TextGetOptions.FormatRtf, randAccStream);
                        }

                    // Let Windows know that we're finished changing the file so the
                    // other app can update the remote version of the file.
                    FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                    if (status != FileUpdateStatus.Complete)
                    {
                        Windows.UI.Popups.MessageDialog errorBox = new("File " + file.Name + " couldn't be saved.");
                        await errorBox.ShowAsync();
                    }
                    saved = true;
                    fileNameWithPath = file.Path;
                    AppTitle.Text = file.Name + " - " + appTitleStr;
                    Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList.Add(file);
                }
            }
            else if (!isCopy || fileName != "Untitled")
            {
                string path = fileNameWithPath.Replace("\\" + fileName, "");
                try
                {
                    StorageFile file = await Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.GetFileAsync("CurrentlyOpenFile");
                    if (file != null)
                    {
                        // Prevent updates to the remote version of the file until we
                        // finish making changes and call CompleteUpdatesAsync.
                        CachedFileManager.DeferUpdates(file);
                        // write to file
                        using (IRandomAccessStream randAccStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                            if (file.Name.EndsWith(".txt"))
                            {
                                editor.Document.SaveToStream(TextGetOptions.None, randAccStream);
                            }
                            else
                            {
                                editor.Document.SaveToStream(TextGetOptions.FormatRtf, randAccStream);
                            }


                        // Let Windows know that we're finished changing the file so the
                        // other app can update the remote version of the file.
                        FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                        if (status != FileUpdateStatus.Complete)
                        {
                            Windows.UI.Popups.MessageDialog errorBox = new("File " + file.Name + " couldn't be saved.");
                            await errorBox.ShowAsync();
                        }
                        saved = true;
                        AppTitle.Text = file.Name + " - " + appTitleStr;
                        Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Remove("CurrentlyOpenFile");
                    }
                }
                catch (Exception)
                {
                    SaveFile(true);
                }
            }
        }

        private async void Print_Click(object sender, RoutedEventArgs e)
        {
            if (PrintManager.IsSupported())
            {
                try
                {
                    // Show print UI
                    await PrintManager.ShowPrintUIAsync();
                }
                catch
                {
                    // Printing cannot proceed at this time
                    ContentDialog noPrintingDialog = new()
                    {
                        Title = "Printing error",
                        Content = "Sorry, printing can't proceed at this time.",
                        PrimaryButtonText = "OK"
                    };
                    await noPrintingDialog.ShowAsync();
                }
            }
            else
            {
                // Printing is not supported on this device
                ContentDialog noPrintingDialog = new()
                {
                    Title = "Printing not supported",
                    Content = "Sorry, printing is not supported on this device.",
                    PrimaryButtonText = "OK"
                };
                await noPrintingDialog.ShowAsync();
            }
        }

        private void BoldButton_Click(object sender, RoutedEventArgs e)
        {
            editor.FormatSelected(RichEditHelpers.FormattingMode.Bold);
            comments.FormatSelected(RichEditHelpers.FormattingMode.Bold);
        }

        private async void NewDoc_Click(object sender, RoutedEventArgs e)
        {
            ApplicationView currentAV = ApplicationView.GetForCurrentView();
            CoreApplicationView newAV = CoreApplication.CreateNewView();
            await newAV.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                var newWindow = Window.Current;
                var newAppView = ApplicationView.GetForCurrentView();
                newAppView.Title = $"Untitled - {Strings.Resources.AppName}";

                var frame = new Frame();
                frame.Navigate(typeof(NonTabbedMainPage));
                newWindow.Content = frame;
                newWindow.Activate();

                await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newAppView.Id,
                    ViewSizePreference.UseMinimum, currentAV.Id, ViewSizePreference.UseMinimum);
            });
        }

        private void StrikethoughButton_Click(object sender, RoutedEventArgs e)
        {
            editor.FormatSelected(RichEditHelpers.FormattingMode.Strikethrough);
        }

        private void SubscriptButton_Click(object sender, RoutedEventArgs e)
        {
            editor.FormatSelected(RichEditHelpers.FormattingMode.Subscript);
        }

        private void SuperScriptButton_Click(object sender, RoutedEventArgs e)
        {
            editor.FormatSelected(RichEditHelpers.FormattingMode.Superscript);
        }

        private void AlignRightButton_Click(object sender, RoutedEventArgs e)
        {
            editor.AlignSelectedTo(RichEditHelpers.AlignMode.Right);
        }

        private void AlignCenterButton_Click(object sender, RoutedEventArgs e)
        {
            editor.AlignSelectedTo(RichEditHelpers.AlignMode.Center);
        }

        private void AlignLeftButton_Click(object sender, RoutedEventArgs e)
        {
            editor.AlignSelectedTo(RichEditHelpers.AlignMode.Left);
        }

        private void FindBoxHighlightMatches()
        {
            FindBoxRemoveHighlights();

            Color highlightBackgroundColor = (Color)Application.Current.Resources["SystemColorHighlightColor"];
            Color highlightForegroundColor = (Color)Application.Current.Resources["SystemColorHighlightTextColor"];

            string textToFind = findBox.Text;
            if (textToFind != null)
            {
                ITextRange searchRange = editor.Document.GetRange(0, 0);
                while (searchRange.FindText(textToFind, TextConstants.MaxUnitCount, FindOptions.None) > 0)
                {
                    searchRange.CharacterFormat.BackgroundColor = highlightBackgroundColor;
                    searchRange.CharacterFormat.ForegroundColor = highlightForegroundColor;
                }
            }
        }

        private void FindBoxRemoveHighlights()
        {
            ITextRange documentRange = editor.Document.GetRange(0, TextConstants.MaxUnitCount);
            SolidColorBrush defaultBackground = editor.Background as SolidColorBrush;
            SolidColorBrush defaultForeground = editor.Foreground as SolidColorBrush;

            documentRange.CharacterFormat.BackgroundColor = defaultBackground.Color;
            documentRange.CharacterFormat.ForegroundColor = defaultForeground.Color;
        }


        private void ItalicButton_Click(object sender, RoutedEventArgs e)
        {
            editor.FormatSelected(RichEditHelpers.FormattingMode.Italic);
            comments.FormatSelected(RichEditHelpers.FormattingMode.Italic);
        }

        private void UnderlineButton_Click(object sender, RoutedEventArgs e)
        {
            editor.FormatSelected(RichEditHelpers.FormattingMode.Underline);
            comments.FormatSelected(RichEditHelpers.FormattingMode.Underline);
        }

        private async void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            // Open a text file.
            FileOpenPicker open = new()
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };

            open.FileTypeFilter.Add(".rtf");
            open.FileTypeFilter.Add(".txt");

            StorageFile file = await open.PickSingleFileAsync();

            if (file != null)
            {
                using (IRandomAccessStream randAccStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    IBuffer buffer = await FileIO.ReadBufferAsync(file);
                    var reader = DataReader.FromBuffer(buffer);
                    reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                    string text = reader.ReadString(buffer.Length);
                    // Load the file into the Document property of the RichEditBox.
                    editor.Document.LoadFromStream(TextSetOptions.FormatRtf, randAccStream);
                    editor.Document.GetText(TextGetOptions.UseObjectText, out originalDocText);
                    //editor.Document.SetText(Windows.UI.Text.TextSetOptions.FormatRtf, text);
                    (BasePage.Current.Tabs.TabItems[BasePage.Current.Tabs.SelectedIndex] as TabViewItem).Header = file.Name;
                    AppTitle.Text = file.Name + " - " + appTitleStr;
                    fileNameWithPath = file.Path;
                }
                saved = true;
                _wasOpen = true;
                Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList.Add(file);
                Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.AddOrReplace("CurrentlyOpenFile", file);
            }
        }

        private async void AddImageButton_Click(object sender, RoutedEventArgs e)
        {
            // Open an image file.
            FileOpenPicker open = new();
            open.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            open.FileTypeFilter.Add(".png");
            open.FileTypeFilter.Add(".jpg");
            open.FileTypeFilter.Add(".jpeg");

            StorageFile file = await open.PickSingleFileAsync();

            if (file != null)
            {
                using IRandomAccessStream randAccStream = await file.OpenAsync(FileAccessMode.Read);
                var properties = await file.Properties.GetImagePropertiesAsync();
                int width = (int)properties.Width;
                int height = (int)properties.Height;

                ImageOptionsDialog dialog = new()
                {
                    DefaultWidth = width,
                    DefaultHeight = height
                };

                ContentDialogResult result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    editor.Document.Selection.InsertImage((int)dialog.DefaultWidth, (int)dialog.DefaultHeight, 0, VerticalCharacterAlignment.Baseline, string.IsNullOrWhiteSpace(dialog.Tag) ? "Image" : dialog.Tag, randAccStream);
                    return;
                }

                // Insert an image
                editor.Document.Selection.InsertImage(width, height, 0, VerticalCharacterAlignment.Baseline, "Image", randAccStream);
            }
        }

        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            // Extract the color of the button that was clicked.
            Button clickedColor = (Button)sender;
            var borderone = (Windows.UI.Xaml.Controls.Border)clickedColor.Content;
            var bordertwo = (Windows.UI.Xaml.Controls.Border)borderone.Child;
            var rectangle = (Windows.UI.Xaml.Shapes.Rectangle)bordertwo.Child;
            var color = (rectangle.Fill as SolidColorBrush).Color;
            editor.Document.Selection.CharacterFormat.ForegroundColor = color;
            //FontColorMarker.SetValue(ForegroundProperty, new SolidColorBrush(color));
            editor.Focus(FocusState.Keyboard);
        }

        private void fontcolorsplitbutton_Click(Microsoft.UI.Xaml.Controls.SplitButton sender, Microsoft.UI.Xaml.Controls.SplitButtonClickEventArgs args)
        {
            // If you see this, remind me to look into the splitbutton color applying logic
        }

        private void AddLinkButton_Click(object sender, RoutedEventArgs e)
        {
            if (Windows.Foundation.Metadata.ApiInformation.IsPropertyPresent("Windows.UI.Xaml.FrameworkElement", "AllowFocusOnInteraction"))
                hyperlinkText.AllowFocusOnInteraction = true;
            editor.Document.Selection.Link = $"\"{hyperlinkText.Text}\"";
            editor.Document.Selection.CharacterFormat.ForegroundColor = (Color)XamlBindingHelper.ConvertValue(typeof(Color), "#6194c7");
            AddLinkButton.Flyout.Hide();
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            editor.Document.Selection.Copy();
        }

        private void CutButton_Click(object sender, RoutedEventArgs e)
        {
            editor.Document.Selection.Cut();
        }

        private void PasteButton_Click(object sender, RoutedEventArgs e)
        {
            editor.Document.Selection.Paste(0);
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            editor.Document.Undo();
        }

        private void RedoButton_Click(object sender, RoutedEventArgs e)
        {
            editor.Document.Redo();
        }

        private Task DisplayAboutDialog()
        {
            AboutBox.Open();
            return Task.CompletedTask;
        }

        public async Task ShowUnsavedDialog()
        {
            string fileName = AppTitle.Text.Replace(" - " + appTitleStr, "");
            ContentDialog aboutDialog = new()
            {
                Title = "Do you want to save changes to " + fileName + "?",
                Content = "There are unsaved changes, want to save them?",
                CloseButtonText = "Cancel",
                PrimaryButtonText = "Save changes",
                SecondaryButtonText = "No",
                DefaultButton = ContentDialogButton.Primary
            };

            aboutDialog.CloseButtonClick += (s, e) => BasePage.Current._openDialog = false;

            ContentDialogResult result = await aboutDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                SaveFile(true);
            }
            else if (result == ContentDialogResult.Secondary)
            {
                await ApplicationView.GetForCurrentView().TryConsolidateAsync();
            }
        }

        private async void AboutBtn_Click(object sender, RoutedEventArgs e)
        {
            await DisplayAboutDialog();
        }

        private void FontsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (editor.Document.Selection != null)
            {
                editor.Document.Selection.CharacterFormat.Name = FontsCombo.SelectedValue.ToString();
            }
        }

        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            FindBoxHighlightMatches();
        }

        private void editor_TextChanged(object sender, RoutedEventArgs e)
        {
            editor.Document.GetText(TextGetOptions.UseObjectText, out string textStart);

            if (textStart == "" || string.IsNullOrWhiteSpace(textStart) || _wasOpen)
            {
                saved = true;
            }
            else
            {
                saved = false;
            }

            if (!saved) UnsavedTextBlock.Visibility = Visibility.Visible;
            else UnsavedTextBlock.Visibility = Visibility.Collapsed;
        }

        private async void Exit_Click(object sender, RoutedEventArgs e)
        {
            if (saved)
            {
                await ApplicationView.GetForCurrentView().TryConsolidateAsync();
            }
            else await ShowUnsavedDialog();
        }

        private void ConfirmColor_Click(object sender, RoutedEventArgs e)
        {
            // Confirm color picker choice and apply color to text
            Color color = myColorPicker.Color;
            editor.Document.Selection.CharacterFormat.ForegroundColor = color;

            // Hide flyout
            colorPickerButton.Flyout.Hide();
        }

        private void CancelColor_Click(object sender, RoutedEventArgs e)
        {
            // Cancel flyout
            colorPickerButton.Flyout.Hide();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is IActivatedEventArgs args)
            {
                if (args.Kind == ActivationKind.File)
                {
                    var fileArgs = args as FileActivatedEventArgs;
                    StorageFile file = (StorageFile)fileArgs.Files[0];
                    using (IRandomAccessStream randAccStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        IBuffer buffer = await FileIO.ReadBufferAsync(file);
                        var reader = DataReader.FromBuffer(buffer);
                        reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                        string text = reader.ReadString(buffer.Length);
                        // Load the file into the Document property of the RichEditBox.
                        editor.Document.LoadFromStream(TextSetOptions.FormatRtf, randAccStream);
                        //editor.Document.SetText(Windows.UI.Text.TextSetOptions.FormatRtf, text);
                        AppTitle.Text = file.Name + " - " + appTitleStr;
                        fileNameWithPath = file.Path;
                    }
                    saved = true;
                    fileNameWithPath = file.Path;
                    Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList.Add(file);
                    Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.AddOrReplace("CurrentlyOpenFile", file);
                    _wasOpen = true;
                }
            }
        }

        private async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            /*SettingsDialog dlg = new(editor, FontsCombo, this);
            await dlg.ShowAsync();*/

            if (Window.Current.Content is Frame rootFrame)
            {
                rootFrame.Navigate(typeof(SettingsPage));
            }
        }

        private void RemoveHighlightButton_Click(object sender, RoutedEventArgs e)
        {
            FindBoxRemoveHighlights();
        }

        private void ReplaceSelected_Click(object sender, RoutedEventArgs e)
        {
            editor.Replace(false, replaceBox.Text);
        }

        private void ReplaceAll_Click(object sender, RoutedEventArgs e)
        {
            editor.Replace(true, find: findBox.Text, replace: replaceBox.Text);
        }

        private void FontSizeBox_ValueChanged(Microsoft.UI.Xaml.Controls.NumberBox sender, Microsoft.UI.Xaml.Controls.NumberBoxValueChangedEventArgs args)
        {
            if (editor != null && editor.Document.Selection != null)
            {
                editor.ChangeFontSize((float)sender.Value);
            }
        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            if (Window.Current.Content is Frame rootFrame)
            {
                rootFrame.Navigate(typeof(HomePage));
            }
        }

        private async void CompactOverlayBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                if (ApplicationView.GetForCurrentView().ViewMode == ApplicationViewMode.CompactOverlay)
                {
                    await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.Default);
                    (button.Content as FontIcon).Glyph = "\uEE49";
                    button.Margin = new(10, 5, 195, 10);
                }
                else
                {
                    ViewModePreferences preferences = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                    preferences.CustomSize = new Windows.Foundation.Size(400, 400);
                    await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, preferences);
                    (button.Content as FontIcon).Glyph = "\uEE47";
                    button.Margin = new(10, 5, 70, 10);
                }
            }
        }

        private async void uteverclick(object sender, RoutedEventArgs e)
        {
            utever dialog = new utever();

            dialog.DefaultButton = ContentDialogButton.Primary;


            var result = await dialog.ShowAsync();
        }

        private void FindButton2_Click(object sender, RoutedEventArgs e)
        {
            textsplitview.IsPaneOpen = true;
        }

        private void closepane(object sender, RoutedEventArgs e)
        {
            textsplitview.IsPaneOpen = false;
        }

        private void RichEditBox_TextChanged(object sender, RoutedEventArgs e)
        {
            editor.Document.GetText(TextGetOptions.UseObjectText, out string textStart);

            if (textStart == "" || string.IsNullOrWhiteSpace(textStart))
            {
                saved = true;
            }
            else
            {
                saved = false;
            }

            if (!saved) UnsavedTextBlock.Visibility = Visibility.Visible;
            else UnsavedTextBlock.Visibility = Visibility.Collapsed;

        }

        private void showinsiderinfo(object sender, RoutedEventArgs e)
        {
            ToggleThemeTeachingTip1.IsOpen = true;
        }

        private void OnKeyboardAcceleratorInvoked(Windows.UI.Xaml.Input.KeyboardAccelerator sender, Windows.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
        {
            switch (sender.Key)
            {
                case Windows.System.VirtualKey.B:
                    editor.FormatSelected(RichEditHelpers.FormattingMode.Bold);
                    BoldButton.IsChecked = editor.Document.Selection.CharacterFormat.Bold == FormatEffect.On;
                    args.Handled = true;
                    break;
                case Windows.System.VirtualKey.I:
                    editor.FormatSelected(RichEditHelpers.FormattingMode.Italic);
                    ItalicButton.IsChecked = editor.Document.Selection.CharacterFormat.Italic == FormatEffect.On;
                    args.Handled = true;
                    break;
                case Windows.System.VirtualKey.U:
                    editor.FormatSelected(RichEditHelpers.FormattingMode.Underline);
                    UnderlineButton.IsChecked = editor.Document.Selection.CharacterFormat.Underline == UnderlineType.Single;
                    args.Handled = true;
                    break;
                case Windows.System.VirtualKey.S:
                    SaveFile(false);
                    break;
            }
        }

        private void editor_SelectionChanged(object sender, RoutedEventArgs e)
        {
            var ST = editor.Document.Selection;
            BoldButton.IsChecked = editor.Document.Selection.CharacterFormat.Bold == FormatEffect.On;
            ItalicButton.IsChecked = editor.Document.Selection.CharacterFormat.Italic == FormatEffect.On;
            UnderlineButton.IsChecked = editor.Document.Selection.CharacterFormat.Underline == UnderlineType.Single;
            //Selected words
            if (ST.Length > 0 || ST.Length < 0)
            {
                SelWordGrid.Visibility = Visibility.Visible;
                editor.Document.Selection.GetText(TextGetOptions.None, out var seltext);
                var selwordcount = seltext.Split(new char[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                SelWordCount.Text = $"Selected words: {selwordcount}";
            }
            else
            {
                SelWordGrid.Visibility = Visibility.Collapsed;
            }
            editor.Document.GetText(TextGetOptions.None, out var text);
            if (text.Length > 0)
            {
                var wordcount = text.Split(new char[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                WordCount.Text = $"Word count: {wordcount}";
            }
            else
            {
                WordCount.Text = $"Word count: 0";
            }
        }

        //To see this code in action, add a call to ShareSourceLoad to your constructor or other
        //initializing function.
        private void ShareSourceLoad()
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(this.DataRequested);
        }

        private void DataRequested(DataTransferManager sender, DataRequestedEventArgs e)
        {
            DataRequest request = e.Request;
            request.Data.Properties.Title = "UltraTextEdit Share Service";
            request.Data.Properties.Description = "Text sharing for the UTE UWP app";
            request.Data.SetText(editor.TextDocument.ToString());
        }

        private void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            ShareSourceLoad();
            DataTransferManager.ShowShareUI();
        }

        private void CommentsButton_Click(object sender, RoutedEventArgs e)
        {
            commentsplitview.IsPaneOpen = true;
            commentstabitem.Visibility = Visibility.Visible;
        }

        private void closecomments(object sender, RoutedEventArgs e)
        {
            commentsplitview.IsPaneOpen = false;
            commentstabitem.Visibility = Visibility.Collapsed;
        }

        /* Method to create a table format string which can directly be set to 
   RichTextBoxControl. Rows, columns and cell width are passed as parameters 
   rather than hard coding as in previous example.*/
        private String InsertTableInRichTextBox(int rows, int cols, int width)
        {
            //Create StringBuilder Instance
            StringBuilder strTableRtf = new StringBuilder();

            //beginning of rich text format
            strTableRtf.Append(@"{\rtf1 ");

            //Variable for cell width
            int cellWidth;

            //Start row
            strTableRtf.Append(@"\trowd");

            //Loop to create table string
            for (int i = 0; i < rows; i++)
            {
                strTableRtf.Append(@"\trowd");

                for (int j = 0; j < cols; j++)
                {
                    //Calculate cell end point for each cell
                    cellWidth = (j + 1) * width;

                    //A cell with width 1000 in each iteration.
                    strTableRtf.Append(@"\cellx" + cellWidth.ToString());
                }

                //Append the row in StringBuilder
                strTableRtf.Append(@"\intbl \cell \row");
            }
            strTableRtf.Append(@"\pard");
            strTableRtf.Append(@"}");
            var strTableString = strTableRtf.ToString();
            editor.Document.Selection.SetText(TextSetOptions.FormatRtf, strTableString);
            return strTableString;

        }



        private async void AddTableButton_Click(object sender, RoutedEventArgs e)
        {
            var dialogtable = new TableDialog();
            await dialogtable.ShowAsync();
            InsertTableInRichTextBox(dialogtable.rows, dialogtable.columns, 1000);
        }

        private void AddSymbolButton_Click(object sender, RoutedEventArgs e)
        {
            //symbolsflyout.AllowFocusOnInteraction = true;
            //symbolsflyout.IsOpen = true;
        }

        private void SymbolButton_Click(object sender, RoutedEventArgs e)
        {
            // Extract the symbol of the button that was clicked.
            Button clickedSymbol = (Button)sender;
            string rectangle = clickedSymbol.Content.ToString();
            string text = rectangle;

            var myDocument = editor.Document;
            string oldText;
            myDocument.GetText(TextGetOptions.None, out oldText);
            myDocument.SetText(TextSetOptions.None, oldText + text);

            symbolbut.Flyout.Hide();
            editor.Focus(FocusState.Keyboard);
        }

        private async void NewInstance_Click(object sender, RoutedEventArgs e)
        {
            ApplicationView currentAV = ApplicationView.GetForCurrentView();
            CoreApplicationView newAV = CoreApplication.CreateNewView();
            await newAV.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                var newWindow = Window.Current;
                var newAppView = ApplicationView.GetForCurrentView();
                newAppView.Title = $"Untitled - {Strings.Resources.AppName}";

                var frame = new Frame();
                frame.Navigate(typeof(BasePage));
                newWindow.Content = frame;
                newWindow.Activate();

                await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newAppView.Id,
                    ViewSizePreference.UseMinimum, currentAV.Id, ViewSizePreference.UseMinimum);
            });
        }

        private async void DateInsertionAsync(object sender, RoutedEventArgs e)
        { // Create a ContentDialog
            ContentDialog dialog = new ContentDialog();
            dialog.Title = "Insert current date and time";

            // Create a ListView for the user to select the date format
            ListView listView = new ListView();
            listView.SelectionMode = ListViewSelectionMode.Single;

            // Create a list of date formats to display in the ListView
            List<string> dateFormats = new List<string>();
            dateFormats.Add(DateTime.Now.ToString("dd.M.yyyy"));
            dateFormats.Add(DateTime.Now.ToString("M.dd.yyyy"));
            dateFormats.Add(DateTime.Now.ToString("dd MMM yyyy"));
            dateFormats.Add(DateTime.Now.ToString("dddd, dd MMMM yyyy"));
            dateFormats.Add(DateTime.Now.ToString("dd MMMM yyyy"));
            dateFormats.Add(DateTime.Now.ToString("hh:mm:ss tt"));
            dateFormats.Add(DateTime.Now.ToString("HH:mm:ss"));
            dateFormats.Add(DateTime.Now.ToString("dddd, dd MMMM yyyy, HH:mm:ss"));
            dateFormats.Add(DateTime.Now.ToString("dd MMMM yyyy, HH:mm:ss"));
            dateFormats.Add(DateTime.Now.ToString("MMM dd, yyyy"));

            // Set the ItemsSource of the ListView to the list of date formats
            listView.ItemsSource = dateFormats;

            // Set the content of the ContentDialog to the ListView
            dialog.Content = listView;

            // Make the insert button colored
            dialog.DefaultButton = ContentDialogButton.Primary;

            // Add an "Insert" button to the ContentDialog
            dialog.PrimaryButtonText = "OK";
            dialog.PrimaryButtonClick += (s, args) =>
            {
                string selectedFormat = listView.SelectedItem as string;
                string formattedDate = dateFormats[listView.SelectedIndex];
                editor.Document.Selection.Text = formattedDate;
            };

            // Add a "Cancel" button to the ContentDialog
            dialog.SecondaryButtonText = "Cancel";

            // Show the ContentDialog
            await dialog.ShowAsync();
        }

        private async void fr_invoke(object sender, RoutedEventArgs e)
        {
            var dialog = new FirstRunDialog();
            dialog.ShowAsync();
        }

        private async void WN_invoke(object sender, RoutedEventArgs e)
        {
            var dialog = new WhatsNewDialog();
            dialog.ShowAsync();
        }

        private void NoneNumeral_Click(object sender, RoutedEventArgs e)
        {
            editor.Document.Selection.ParagraphFormat.ListType = MarkerType.None;
            myListButton.IsChecked = false;
            myListButton.Flyout.Hide();
            editor.Focus(FocusState.Keyboard);
        }

        private void DottedNumeral_Click(object sender, RoutedEventArgs e)
        {
            editor.Document.Selection.ParagraphFormat.ListType = MarkerType.Bullet;
            myListButton.IsChecked = true;
            myListButton.Flyout.Hide();
            editor.Focus(FocusState.Keyboard);
        }

        private void NumberNumeral_Click(object sender, RoutedEventArgs e)
        {
            editor.Document.Selection.ParagraphFormat.ListType = MarkerType.Arabic;
            myListButton.IsChecked = true;
            myListButton.Flyout.Hide();
            editor.Focus(FocusState.Keyboard);
        }

        private void LetterSmallNumeral_Click(object sender, RoutedEventArgs e)
        {
            editor.Document.Selection.ParagraphFormat.ListType = MarkerType.LowercaseEnglishLetter;
            myListButton.IsChecked = true;
            myListButton.Flyout.Hide();
            editor.Focus(FocusState.Keyboard);
        }

        private void LetterBigNumeral_Click(object sender, RoutedEventArgs e)
        {
            editor.Document.Selection.ParagraphFormat.ListType = MarkerType.UppercaseEnglishLetter;
            myListButton.IsChecked = true;
            myListButton.Flyout.Hide();
            editor.Focus(FocusState.Keyboard);
        }

        private void SmalliNumeral_Click(object sender, RoutedEventArgs e)
        {
            editor.Document.Selection.ParagraphFormat.ListType = MarkerType.LowercaseRoman;
            myListButton.IsChecked = true;
            myListButton.Flyout.Hide();
            editor.Focus(FocusState.Keyboard);
        }

        private void BigINumeral_Click(object sender, RoutedEventArgs e)
        {
            editor.Document.Selection.ParagraphFormat.ListType = MarkerType.UppercaseRoman;
            myListButton.IsChecked = true;
            myListButton.Flyout.Hide();
            editor.Focus(FocusState.Keyboard);
        }

        private void BackPicker_ColorChanged(object Sender, Windows.UI.Xaml.Controls.ColorChangedEventArgs EvArgs)
        {
            //Configure font highlight
            if (!(editor == null))
            {
                var ST = editor.Document.Selection;
                if (!(ST == null))
                {
                    _ = ST.CharacterFormat;
                    var Br = new SolidColorBrush(BackPicker.Color);
                    var CF = BackPicker.Color;
                    if (BackAccent != null) BackAccent.Foreground = Br;
                    ST.CharacterFormat.BackgroundColor = CF;
                }
            }
        }

        private void HighlightButton_Click(object Sender, RoutedEventArgs EvArgs)
        {
            //Configure font color
            var BTN = Sender as Button;
            var ST = editor.Document.Selection;
            if (!(ST == null))
            {
                _ = ST.CharacterFormat.ForegroundColor;
                var Br = BTN.Foreground;
                BackAccent.Foreground = Br;
                ST.CharacterFormat.BackgroundColor = (BTN.Foreground as SolidColorBrush).Color;
            }
        }

        private void NullHighlightButton_Click(object Sender, RoutedEventArgs EvArgs)
        {
            //Configure font color
            var ST = editor.Document.Selection;
            if (!(ST == null))
            {
                _ = ST.CharacterFormat.ForegroundColor;
                BackAccent.Foreground = new SolidColorBrush(Colors.Transparent);
                ST.CharacterFormat.BackgroundColor = Colors.Transparent;
            }
        }

        private void HyperlinkButton_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void HyperlinkButton_Click_2(object sender, RoutedEventArgs e)
        {

        }

        private void HyperlinkButton_Click_3(object sender, RoutedEventArgs e)
        {

        }

        private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void find_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            FindBoxHighlightMatches();
        }

        private void replace_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            editor.Replace(false, replace.Text);
        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            if (ReplacePanel.Visibility == Visibility.Visible)
            {
                ReplacePanel.Visibility = Visibility.Collapsed;
                buticon.Glyph = "\uE7B3";
                ToolTipService.SetToolTip(replacecontrol, "Show Replace box");
            }
            else
            {
                ReplacePanel.Visibility = Visibility.Visible;
                buticon.Glyph = "\uED1A";
                ToolTipService.SetToolTip(replacecontrol, "Hide Replace box");
            }
        }
    }
}
