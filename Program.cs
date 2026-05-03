using DraftosaurusClient.Forms;

namespace DraftosaurusClient;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new FormLobby());
    }
}
