using active_directory_wpf_msgraph_v2.Migrations;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace active_directory_wpf_msgraph_v2.Helppers
{
    class MainWindowFunctions
    {
        public static async Task GoogleAsync(Window win, TextBox Usuario, PasswordBox Senha)
        {
            new Output("100", "inicializando conectores: Microsoft:");
            Connectors.MsGraph MsService = await Connectors.MsGraph.InitGraphAsync(win, Usuario, Senha);
            new Output("200", $"inicializando conectores: Google:");
            Connectors.GoogleC GoogleService = new Connectors.GoogleC();
            GoogleService.connection();
            List<Microsoft.Graph.Calendar> calendars = await GoogleService.calendarAsync(MsService);
            new Output("1100", "Processo de migração de calendarios iniciado:");
                  foreach (Microsoft.Graph.Calendar temp in calendars)
                  {
                      await MigrateCalendars.CloudUpAsync(GoogleService.events(temp), temp, MsService);
                  }
            new Output("2000", "Processo de migração de emails iniciado");
            await MigrateGmailAPI.processAsync(GoogleService, MsService);
            new Output("3000", "Processo de migração de contatos iniciado");
            await GoogleService.contacsAsync(MsService);

        }
        private static async Task logOff()
        {
            System.IO.File.Delete("token.json\\credentials.json");
            var accounts = await App.PublicClientApp.GetAccountsAsync();
            if (accounts.Any())
            {
                try
                {
                    await App.PublicClientApp.RemoveAsync(accounts.FirstOrDefault());
                    new Output("149", "User has signed-out");
                }
                catch (MsalException ex)
                {
                    new Output("199", $"Error signing-out user: {ex.Message}");
                }
            }
        }
    }
}
