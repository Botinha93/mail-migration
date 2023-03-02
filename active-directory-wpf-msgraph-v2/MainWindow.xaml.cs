using active_directory_wpf_msgraph_v2.Helppers;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace active_directory_wpf_msgraph_v2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        String selected = String.Empty;
        //Set the API Endpoint to Graph 'me' endpoint. 
        // To change from Microsoft public cloud to a national cloud, use another value of graphAPIEndpoint.
        // Reference with Graph endpoints here: https://docs.microsoft.com/graph/deployments#microsoft-graph-and-graph-explorer-service-root-endpoints
        static ListView _listView = new ListView();
        private bool dragging;
        private Point startPoint;

        public MainWindow()
        {
            InitializeComponent();
            output.ItemsSource = new List<Output>();
            port.KeyDown += textBox1_KeyPress;
            port.KeyUp += textBox1_KeyPress;
            _listView = output;
        }
        
        public static void RefreshListview() 
        {
            try
            {
                _listView.ItemsSource = null;
                _listView.ItemsSource = Output.registry;
            }
            catch { }
        }
        private void SelectedEmail(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            selected = ((e.Source as System.Windows.Controls.ComboBox).SelectedItem as System.Windows.Controls.ComboBoxItem).Content.ToString();
            header.Header = "Configurações SMTP/API " + selected;
            if (selected.Contains("Google"))
            {
                setGoogle();
            }
            else
            {
                Reset();
            }
        }
        private void setGoogle()
        {
            Reset();
            port.Text = "993";
            tsl.IsChecked = true;
            ssl.IsChecked = true;
            apiurl.Text = String.Empty;
            smtp.Text = "imap.gmail.com";
            port.IsEnabled = false;
            tsl.IsEnabled = false;
            ssl.IsEnabled = false;
            apiurl.IsEnabled = false;
            smtp.IsEnabled = false;
            Usuario1.IsEnabled = false;
            Senha1.IsEnabled = false;
        }
        private void Reset()
        {
            port.Text = String.Empty;
            tsl.IsChecked = false;
            ssl.IsChecked = false;
            apiurl.Text = String.Empty;
            smtp.Text = String.Empty;
            port.IsEnabled = true;
            tsl.IsEnabled = true;
            ssl.IsEnabled = true;
            apiurl.IsEnabled = true;
            smtp.IsEnabled = true;
            Usuario1.IsEnabled = true;
            Senha1.IsEnabled = true;
        }
        private void textBox1_KeyPress(object sender, EventArgs e)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(port.Text, "[^0-9]"))
            {
                port.Text = port.Text.Remove(port.Text.Length - 1);
            }
        }
        /// <summary>
        /// Call AcquireToken - to acquire a token requiring user to sign-in
        /// </summary>
        private void CallGraphButton_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrEmpty(selected))
            {
                if (selected.Contains("Google")) {
                    CallGraphButton.IsEnabled = false;
                    _ = MainWindowFunctions.GoogleAsync(this, Usuario, Senha);
                }
            }
            /*migrateIMAP();*/
        }

        void migrateIMAP()
        {
            new Output("2000", "Iniciando migração de emails");
            Migrations.MigrateEmialsIMAP.Perform(
                Migrations.MigrateEmialsIMAP.Connect("outlook.office365.com", Usuario.Text, Senha.Password, 993, true, true),
                Migrations.MigrateEmialsIMAP.Connect(smtp.Text, Usuario1.Text, Senha1.Password, int.Parse(port.Text), (bool)tsl.IsChecked, (bool)ssl.IsChecked)
                );
            new Output("2049", "Finalizada migração de emails");
        }

            /// <summary>
            /// Perform an HTTP GET request to a URL using an HTTP Authorization header
            /// </summary>
            /// <param name="url">The URL</param>
            /// <param name="token">The token</param>
            /// <returns>String containing the results of the GET operation</returns>
            public async Task<string> GetHttpContentWithToken(string url, string token)
        {
            var httpClient = new System.Net.Http.HttpClient();
            System.Net.Http.HttpResponseMessage response;
            try
            {
                var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
                //Add the token in Authorization header
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                response = await httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                return content;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        /// <summary>
        /// Display basic information contained in the token
        /// </summary>
        private void DisplayBasicTokenInfo(AuthenticationResult authResult)
        {
            if (authResult != null)
            {
                new Output("1", $"Username: {authResult.Account.Username}" + Environment.NewLine);
                new Output("1", $"Token Expires: {authResult.ExpiresOn.ToLocalTime()}" + Environment.NewLine);
            }

        }

    private void Output_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                Point p = PointToScreen(new Point(Mouse.GetPosition(null).X, Mouse.GetPosition(null).Y));
                this.Left = (p.X - this.startPoint.X);
                this.Top = (p.Y - this.startPoint.Y);

            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            dragging = true;
            startPoint = new Point(Mouse.GetPosition(null).X, Mouse.GetPosition(null).Y);
        }

        private void TitleBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            dragging = false;
        }
    }
}
