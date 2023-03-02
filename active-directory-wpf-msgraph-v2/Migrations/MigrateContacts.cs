using active_directory_wpf_msgraph_v2.Connectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace active_directory_wpf_msgraph_v2.Migrations
{
    class MigrateContacts
    {
        static public async Task migrateAsync(MsGraph currentGraph, Microsoft.Graph.Contact contacts)
        {
            try
            {
                await currentGraph.graphServiceClient.Me.Contacts.Request().AddAsync(contacts);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
