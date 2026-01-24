using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using Microsoft.EntityFrameworkCore;
using Courcework.Data;
using Courcework.Services;

namespace Courcework
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddMudServices();

            // Register SQLite Database Services
            builder.Services.AddDbContext<JournalDbContext>();
            
            // Register Secure Storage Service
            builder.Services.AddScoped<ISecureStorageService, SecureStorageService>();
            
            // Register Authentication Service
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
            
            // Register Storage Service
            builder.Services.AddScoped<IStorageService, DatabaseStorageService>();
            
            // Keep StorageService for JSON migration (can be removed after migration)
            builder.Services.AddSingleton<StorageService>();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}

