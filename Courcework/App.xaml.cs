using Courcework.Data;

namespace Courcework
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override async void OnStart()
        {
            base.OnStart();

            // Initialize database on app start
            try
            {
                var dbContext = Application.Current.Handler.MauiContext?.Services.GetService<JournalDbContext>();
                if (dbContext != null)
                {
                    // Ensure database is created
                    await dbContext.Database.EnsureCreatedAsync();
                    System.Diagnostics.Debug.WriteLine("✅ Database initialized successfully");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Database initialization error: {ex.Message}");
            }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new MainPage()) { Title = "Courcework" };
        }
    }
}
