using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using Microsoft.Maui.Controls;

namespace CMLGapp.Helpers
{
    public static class ValidationHelper
    {
        public static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }

        public static bool IsStrongPassword(string password)
        {
            return Regex.IsMatch(password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$");
        }

        public static bool IsValidUserName(string username)
        {
            return Regex.IsMatch(username, @"^[a-zA-Z0-9_.@-]+$");
        }

        public static async Task AnimateValidation(Label label, bool show, string message = "")
        {
            if (label == null) return;

            if (show)
            {
                label.Text = message;
                label.Opacity = 0;
                label.IsVisible = true;
                await label.FadeTo(1, 250);
            }
            else
            {
                await label.FadeTo(0, 200);
                label.IsVisible = false;
            }
        }

        public static void TogglePasswordVisibility(ImageButton toggleIcon, Entry entry)
        {
            if (entry == null || toggleIcon == null) return;

            entry.IsPassword = !entry.IsPassword;
            toggleIcon.Source = entry.IsPassword ? "eye.svg" : "eyeclosed.svg";
        }

        public static void ShowToggleIfNotEmpty(Entry entry, ImageButton toggleIcon)
        {
            if (entry == null || toggleIcon == null) return;

            bool isNotEmpty = !string.IsNullOrWhiteSpace(entry.Text);
            toggleIcon.IsVisible = isNotEmpty;

            if (!isNotEmpty)
            {
                entry.IsPassword = true;
                toggleIcon.Source = "eye.svg";
            }
        }

        // create empty cards
        public static Frame BuildEmptyCard(string message = "🔔 Card is empty")
        {
            return new Frame
            {
                BackgroundColor = Color.FromArgb("#28323C"),
                BorderColor = Color.FromArgb("#28323C"),
                CornerRadius = 10,
                Padding = 10,
                Content = new VerticalStackLayout
                {
                    Children =
                    {
                        new Label { Text = message, FontSize = 16, TextColor = Color.FromArgb("#8C96A0") },
                        new BoxView
                        {
                            HeightRequest = 1, BackgroundColor = Color.FromArgb("#28323C"), HorizontalOptions = LayoutOptions.Fill, Margin = new Thickness(0,5)
                        }
                    }
                }
            };
        }
    }
}
