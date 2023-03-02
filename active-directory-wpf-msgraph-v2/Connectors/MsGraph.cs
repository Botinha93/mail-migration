using active_directory_wpf_msgraph_v2.Helppers;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace active_directory_wpf_msgraph_v2.Connectors
{
    class MsGraph
    {
        public GraphServiceClient graphServiceClient { get; set; }
        public AuthenticationResult authResult { get; set; }
        Window Mainwindow;
        TextBox Usuario;
        PasswordBox Senha;
        string graphAPIEndpoint = "https://graph.microsoft.com/v1.0/me";

        //Set the scope for API call to user.read
        string[] scopes = new string[] { "user.read"
            ,"Calendars.ReadWrite"
            ,"Contacts.ReadWrite"
            ,"email"
            ,"Mail.ReadWrite"
            ,"Notes.ReadWrite.All"
            ,"openid"
            ,"profile"};
        public static async Task<MsGraph> InitGraphAsync(Window Mainwindow, TextBox Usuario, PasswordBox Senha)
        {
            MsGraph thisGraph = new MsGraph();
            thisGraph.Mainwindow = Mainwindow;
            thisGraph.Usuario = Usuario;
            thisGraph.Senha = Senha;
            await thisGraph.Graph();
            return thisGraph;
        }
        private async Task Graph()
        {
            authResult = null;
            var app = App.PublicClientApp;
            var accounts = await app.GetAccountsAsync();
            var firstAccount = accounts.FirstOrDefault();

            try
            {
                authResult = await app.AcquireTokenSilent(scopes, firstAccount)
                    .ExecuteAsync();
            }
            catch (MsalUiRequiredException ex)
            {
                // A MsalUiRequiredException happened on AcquireTokenSilent. 
                // This indicates you need to call AcquireTokenInteractive to acquire a token
                new Output("150", $"MsalUiRequiredException: {ex.Message}");

                try
                {
                    authResult = await app.AcquireTokenByUsernamePassword(scopes, Usuario.Text, Senha.SecurePassword)
                       .ExecuteAsync();

                }
                catch (MsalException msalex)
                {
                    new Output("155", $"Erro ao utilizar o usuario e o password:{System.Environment.NewLine}{msalex}");
                    try
                    {
                        authResult = await app.AcquireTokenInteractive(scopes)
                            .WithAccount(accounts.FirstOrDefault())
                            .WithParentActivityOrWindow(new WindowInteropHelper(Mainwindow).Handle) // optional, used to center the browser on the window
                            .WithPrompt(Microsoft.Identity.Client.Prompt.SelectAccount)
                            .ExecuteAsync();

                    }
                    catch
                    {
                        new Output("160", $"Erro ao adiquirir o token:{System.Environment.NewLine}{msalex}");
                    }
                }
            }
            catch (Exception ex)
            {
                new Output("165", $"Error ao adiquirir Token Silently:{System.Environment.NewLine}{ex}");
                return;
            }

            if (authResult != null)
            {
                graphServiceClient = new GraphServiceClient(new DelegateAuthenticationProvider(async (requestMessage) =>
                           {
                               // Add the access token in the Authorization header of the API request.
                               requestMessage.Headers.Authorization =
                                  new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
                           })
                       );
                new Output("110", $"Usuario autenticado");
                /**ResultText.Text = await GetHttpContentWithToken(graphAPIEndpoint, authResult.AccessToken);**/

            }
            else
            {
                new Output("170", $"Não foi possivel autenticar o usuario");
            }
        }
    }
}
