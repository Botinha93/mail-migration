using active_directory_wpf_msgraph_v2.Helppers;
using AE.Net.Mail;
using AE.Net.Mail.Imap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace active_directory_wpf_msgraph_v2.Migrations
{
    public class MigrateEmialsIMAP : ImapClient
    {
        String URI; String User; String pass; int Port = 993; Boolean tls = false; Boolean ssl = false;
        private MigrateEmialsIMAP(String URI, String User, String pass, int Port , Boolean tls , Boolean ssl ) : base(URI, User, pass,
                                        AuthMethods.Login, Port, tls, !ssl)
        {
            this.URI = URI;
            this.User = User;
            this.pass = pass;
            this.Port = Port;
            this.tls = tls;
            this.ssl = ssl;
            new Output("2010", "Connectado ao " + URI + " como " + User);
        }
        public static MigrateEmialsIMAP Connect(String URI, String User, String pass, int Port = 993,Boolean tls = false, Boolean ssl = false)
        {
            // Connect to the IMAP server. The 'true' parameter specifies to use SSL
            // which is important (for Gmail at least)
            MigrateEmialsIMAP ic = null;
            try
            {
                ic = new MigrateEmialsIMAP(URI, User, pass, Port, tls, ssl);
            }
            catch(Exception e)
            {
                new Output("2050", "Erro ao connectar, motivo:"+ e.Message);
                Console.WriteLine(e.Message);
            }
            // Select a mailbox. Case-insensitive
            return ic;
        }
        public MigrateEmialsIMAP reCoonnect()
        {
            base.Dispose();
            return new MigrateEmialsIMAP(URI, User, pass, Port, tls, !ssl);
        }
        public static void Perform(MigrateEmialsIMAP MS, MigrateEmialsIMAP ToM)
        {
            foreach (Mailbox box in ToM.ListMailboxes("","*"))
            {
                MS.CreateMailbox(box.Name);
                MS.SelectMailbox(box.Name);
                ToM.SelectMailbox(box.Name);
                MailMessage[] mm = ToM.GetMessages(0, ToM.GetMessageCount());
                new Output("2020", "Migrando"+ mm.Length + " mensagens, na caixa " + box.Name);
                foreach (MailMessage m in mm)
                {
                    MS.AppendMail(m, box.Name);
                }
            }
            MS.Dispose();
            ToM.Dispose();
        }

    }
}
