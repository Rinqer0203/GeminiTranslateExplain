namespace GeminiTranslateExplain.Services
{
    public static class ExceptionHandlerManager
    {
        public static void RegisterHandlers()
        {
            System.Windows.Application.Current.DispatcherUnhandledException += (s, e) =>
            {
                ErrorLogger.Log("Dispatcher unhandled exception", e.Exception);
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                ErrorLogger.Log("AppDomain unhandled exception", e.ExceptionObject as Exception);
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                ErrorLogger.Log("Unobserved task exception", e.Exception);
            };
        }
    }
}
