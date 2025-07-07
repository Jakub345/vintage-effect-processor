using System;
using System.Windows.Forms;
namespace JaProj
{
    // Standardowy szablon dla plikacji Windows Forms
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles(); // Kontrolki windows (wygląd standatdowej aplikacji windows)
            Application.SetCompatibleTextRenderingDefault(false); // Domyślny tryb renderowania tekstu (false - użycie nowego systemu renderowania teksty)
            Application.Run(new MainForm()); // Tworzy nową instację okna MainForm (program będzie działął dopóki okno nie zostanie zamknięte)
        }
    }
}