using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using InvisibleManXRay.Windows;

namespace CustomMessageBox;

/// <summary>
/// Interaction logic for MessageBoxCustom.xaml
/// </summary>
public partial class MessageBoxCustom : BaseWindow
{
    public static bool? Show(string text)
    {
        return Show(null, text);
    }
    public static bool? Show(Window owner, string text, string caption = "", MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
    {
        var type = image switch
        {
            MessageBoxImage.Information => MessageType.Info,
            MessageBoxImage.Warning => MessageType.Warning,
            MessageBoxImage.Error => MessageType.Error,
            MessageBoxImage.Question => MessageType.Confirmation,
            _ => MessageType.Info,
        };
        var dialog = new MessageBoxCustom(text, type, button) { Owner = owner };
        if(!string.IsNullOrEmpty(caption))
        {
            dialog.Title = caption;
        }
        return dialog.ShowDialog();
    }
    public Window Owner { get => base.Owner; set { base.Owner = value; } }
    public MessageBoxCustom(string Message, MessageType Type = MessageType.Info, MessageBoxButton Buttons = MessageBoxButton.OK)
    {
        InitializeComponent();
        txtMessage.Text = Message;
        switch (Type)
        {
            case MessageType.Info:
                Title = "Info";
                break;
            case MessageType.Confirmation:
                Title = "Confirmation";
                break;
            case MessageType.Success:
                {
                    string defaultColor = "#4527a0";
                    Color bkColor = (Color)ColorConverter.ConvertFromString(defaultColor);
                    changeBackgroundThemeColor(Colors.Green);
                    Title = "Success";
                }
                break;
            case MessageType.Warning:
                Title = "Warning";
                break;
            case MessageType.Error:
                {
                    string defaultColor = "#F44336";
                    Color bkColor = (Color)ColorConverter.ConvertFromString(defaultColor);
                    changeBackgroundThemeColor(bkColor);
                    changeBackgroundThemeColor(Colors.Red);
                    Title = "Error";
                }
                break;
        }

        switch (Buttons)
        {
            case MessageBoxButton.OKCancel:
                btnYes.Visibility = Visibility.Collapsed;
                btnNo.Visibility = Visibility.Collapsed;
                break;
            case MessageBoxButton.YesNo:
                btnOk.Visibility = Visibility.Collapsed;
                btnCancel.Visibility = Visibility.Collapsed;
                break;
            case MessageBoxButton.OK:
                btnOk.Visibility = Visibility.Visible;
                btnCancel.Visibility = Visibility.Collapsed;
                btnYes.Visibility = Visibility.Collapsed;
                btnNo.Visibility = Visibility.Collapsed;
                break;
            case MessageBoxButton.YesNoCancel:
                btnOk.Visibility = Visibility.Collapsed;
                btnCancel.Visibility = Visibility.Visible;
                btnYes.Visibility = Visibility.Visible;
                btnNo.Visibility = Visibility.Visible;
                break;
        }
    }
    public void changeBackgroundThemeColor(Color newColor)
    {
        btnYes.Background = new SolidColorBrush(newColor);
        btnNo.Background = new SolidColorBrush(newColor);

        btnOk.Background = new SolidColorBrush(newColor);
        btnCancel.Background = new SolidColorBrush(newColor);
    }
    private void btnYes_Click(object sender, RoutedEventArgs e)
    {
        this.DialogResult = true;
        this.Close();
    }

    private void btnCancel_Click(object sender, RoutedEventArgs e)
    {
        this.DialogResult = false;
        this.Close();
    }

    private void btnOk_Click(object sender, RoutedEventArgs e)
    {
        this.DialogResult = true;
        this.Close();
    }

    private void btnNo_Click(object sender, RoutedEventArgs e)
    {
        this.DialogResult = false;
        this.Close();
    }

    private void btnClose_Click(object sender, RoutedEventArgs e)
    {
        this.DialogResult = false;
        this.Close();
    }
}
public enum MessageType
{
    Info,
    Confirmation,
    Success,
    Warning,
    Error,
}
