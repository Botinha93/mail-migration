using active_directory_wpf_msgraph_v2.Connectors;
using active_directory_wpf_msgraph_v2.Helppers;
using AE.Net.Mail;
using AE.Net.Mail.Imap;
using Google.Apis.Gmail.v1.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace active_directory_wpf_msgraph_v2.Migrations
{
    class MigrateGmailAPI
    {
        public static async Task processAsync(Connectors.GoogleC serviceGoogle, MsGraph currentGraph)
        {
            IList<String> messagesBox = serviceGoogle.getMailBoxes();
            await doGoogle( messagesBox, serviceGoogle, currentGraph);
        }
        private static async Task doGoogle(IList<String> ToM, Connectors.GoogleC serviceGoogle, MsGraph currentGraph)
        {
           
            foreach (string box in ToM)
            {
                Microsoft.Graph.IUserMailFoldersCollectionPage folders = await currentGraph.graphServiceClient.Me.MailFolders
                .Request()
                .GetAsync();
                Microsoft.Graph.MailFolder folder = new Microsoft.Graph.MailFolder();
                folder.Id = box;
                try
                {
                    Console.WriteLine(box);
                    foreach (var fol in folders)
                    {
                        if (box.Equals("SENT") && fol.DisplayName.Contains("sentitems"))
                        {
                            folder = fol;
                            break;
                        }
                        else if (box.Equals("SPAM") && fol.DisplayName.Contains("junkemail"))
                        {
                            folder = fol;
                            break;
                        }
                        else if (box.Equals("TRASH") && fol.DisplayName.Contains("deleteditems"))
                        {
                            folder = fol;
                            break;
                        }
                        else if (box.Equals("DRAFT") && fol.DisplayName.Contains("drafts"))
                        {
                            folder = fol;
                            break;
                        }
                    }
                    if (folder == null)
                    {
                        try
                        {
                            folder = (await currentGraph.graphServiceClient.Me.MailFolders
                                    .Request()
                                    .Filter("displayName eq \'" + box + "\'")
                                    .GetAsync())[0];
                        }
                        catch
                        {
                            try
                            {
                                folder = await currentGraph.graphServiceClient.Me.MailFolders
                                        .Request()
                                        .AddAsync(folder);
                            }
                            catch
                            {
                                new Output("2270", "Erro ao recuperar/criar caixa: " + box);
                            }
                        }
                    }
                    await serviceGoogle.getMailAsync(box, folder.Id, currentGraph);

                }
                catch
                {
                    new Output("2260", "Erro ao recuperar caixas, sua conexão está ok? ");
                }
            }
        }
        public static async Task<Microsoft.Graph.Message> cloudUpMessageAsync(MsGraph currentGraph, Microsoft.Graph.Message message, string box)
        {
            return await currentGraph.graphServiceClient.Me.MailFolders[box].Messages
                                .Request()
                                .AddAsync(message);
        }
    }
}
