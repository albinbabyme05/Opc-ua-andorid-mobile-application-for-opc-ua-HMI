using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace CMLGapp.Views
{
    public partial class PasswordPromptPage : ContentPage
    {
        private TaskCompletionSource<string> _tcs;

        public PasswordPromptPage(string title)
        {
            InitializeComponent();
            TitleLabel.Text = title;
        }

        public Task<string> GetPasswordAsync()
        {
            _tcs = new TaskCompletionSource<string>();
            return _tcs.Task;
        }

        private async void OnVerifyClicked(object sender, System.EventArgs e)
        {
            _tcs?.TrySetResult(PasswordEntry.Text);
            await Navigation.PopModalAsync();
        }

        private async void OnCancelClicked(object sender, System.EventArgs e)
        {
            _tcs?.TrySetResult(null);
            await Navigation.PopModalAsync();
        }
    }
}
