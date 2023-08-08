using AltTabCatch;

namespace AltTabCatch
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using KeyHook keyHook = new KeyHook();
            keyHook.KeyEvent += HandleKeyEvents;
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }

        public static void HandleKeyEvents(KeyHookEventArgs e)
        {
            if (e.Key == (Keys.Alt | Keys.Tab))
            {
                e.Handled = true;
            }
        }
    }
}