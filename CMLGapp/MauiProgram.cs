using CommunityToolkit.Maui;
using Microcharts.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using Plugin.LocalNotification;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Syncfusion.Maui.Core.Hosting;

namespace CMLGapp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseLocalNotification()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("ArameMono-0-m.ttf", "zollerTextFont");
                })
                //Register ui Package
                .ConfigureSyncfusionCore()
                .UseSkiaSharp()
                .UseMauiCommunityToolkit()
                .UseMauiCommunityToolkitMediaElement()
                .UseMicrocharts();

            builder.ConfigureMauiHandlers(handlers =>
            {
#if ANDROID
                EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
                {
                    // Remove Android's default underline/background
                    handler.PlatformView.Background = null;
                    handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
                    handler.PlatformView.BackgroundTintList =
                        Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
                });
#endif

                /* 
                #if IOS || MACCATALYST
                                EntryHandler.Mapper.AppendToMapping("NoBorder", (handler, view) =>
                                {
                                    handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
                                    handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
                                });
                #endif
                #if WINDOWS
                                EntryHandler.Mapper.AppendToMapping("NoBorder", (handler, view) =>
                                {
                                    handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
                                    handler.PlatformView.BorderBrush = null;
                                    handler.PlatformView.Background = null;
                                });
                #endif
                */
            }); //

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
