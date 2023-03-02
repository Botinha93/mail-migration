using active_directory_wpf_msgraph_v2.Helppers;
using active_directory_wpf_msgraph_v2.Migrations;
using AE.Net.Mail;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.PeopleService;
using Google.Apis.PeopleService.v1;
using Google.Apis.PeopleService.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using static Google.Apis.Calendar.v3.CalendarListResource;

namespace active_directory_wpf_msgraph_v2.Connectors
{
    class GoogleC
    {
        private const string maskContacts = "addresses,ageRanges,biographies,birthdays,braggingRights,coverPhotos,emailAddresses,events,genders,imClients,interests,locales,memberships,metadata,names,nicknames,occupations,organizations,phoneNumbers,photos,relations,relationshipInterests,relationshipStatuses,residences,sipAddresses,skills,taglines,urls,userDefined";

        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/calendar-dotnet-quickstart.json
        static string[] Scopes = { CalendarService.Scope.CalendarReadonly, GmailService.Scope.GmailModify , PeopleServiceService .Scope.ContactsReadonly};
        static string ApplicationName = "Google Calendar API .NET Quickstart";
        private List<String> idgoogle = new List<String>();
        private List<String> idgoogleName = new List<String>();
        CalendarService calendarservice;
        GmailService mailservice;
        Google.Apis.PeopleService.v1.PeopleServiceService contactsService;
        UserCredential credential;
        public String connection()
        {
            String returnText = "";
            try
            {
                using (var stream =
                    new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
                {
                    // The file token.json stores the user's access and refresh tokens, and is created
                    // automatically when the authorization flow completes for the first time.
                    string credPath = "token.json";
                    credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.Load(stream).Secrets,
                        Scopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore(credPath, true)).Result;
                }
                new Output("200", "Credenciais adiquiridas para: " + credential.UserId);
            }
            catch (Exception e)
            {
                new Output("250", "Não foi possivel adiquirir as credenciais. Verifique a existencia do arquivo \"credentials.json\" ");
            }
            try
            {
                calendarservice = new CalendarService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });
                mailservice = new GmailService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });
                contactsService = new Google.Apis.PeopleService.v1.PeopleServiceService(new  BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });

                new Output("210", "Inicializado serviço ");
            }
            catch (Exception e)
            {
                new Output("260", "Falha ao inicializar o serviço ");
            }
            return returnText;
        }
        /* 
         ********************************************************************************************************
         AREA DE CALENDARIO
         ********************************************************************************************************
         */
        //recupera nome dos calensarios do google e retorna um Ilist de calendarios microsoft
        public async Task<List<Microsoft.Graph.Calendar>> calendarAsync(MsGraph currentGraph)
        {
            CalendarListResource.ListRequest requestCalendars = calendarservice.CalendarList.List();
            requestCalendars.MaxResults = 250;
            CalendarList calendars = requestCalendars.Execute();
            List<Microsoft.Graph.Calendar> returnCalendars = new List<Microsoft.Graph.Calendar>();
            foreach (var calendar in calendars.Items)
            {
                idgoogleName.Add(calendar.Summary);
                idgoogle.Add(calendar.Id);
                if (!calendar.Id.Contains("#contacts") && !calendar.Id.Contains("#holiday"))
                    try
                    {
                        returnCalendars.Add(await MigrateCalendars.CloudUp(MigrateCalendars.buildCalendar(calendar.Summary), currentGraph));
                    } catch
                    {
                        foreach (Microsoft.Graph.Calendar cal in await currentGraph.graphServiceClient.Me.Calendars
                        .Request()
                        .GetAsync())
                        {
                            if (cal.Name.Contains(calendar.Summary))
                            {
                                returnCalendars.Add(cal);
                                break;
                            }
                        }
                    }
            }
            new Output("1210", "Recuperado calendarios google");
            return returnCalendars;
        }
        //recupera eventos do calendario do google e retorna um List e eventos convertidos a microsoft
        public List<Microsoft.Graph.Event> events(Microsoft.Graph.Calendar calendar)
        {
            List<Microsoft.Graph.Event> MSEvents = new List<Microsoft.Graph.Event>();
            // Create Google Calendar API service.
            string token = "";
            EventsResource.ListRequest request = calendarservice.Events.List(getGoogleID(calendar));
            request.ShowDeleted = true;
            request.SingleEvents = true;
            request.MaxResults = 50;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
            new Output("1220", "Recuperando eventos calendario:" + getGoogleID(calendar));
            if (token.Length > 0)
            {
                request.PageToken = token;
            }
            // List events.
            Events events = request.Execute();
            if (events.NextPageToken != null)
            {
                token = events.NextPageToken;
            }

            if (events.Items != null && events.Items.Count > 0)
            {
                foreach (var eventItem in events.Items)
                {
                    MSEvents.Add(MigrateCalendars.convertEvents(eventItem, calendar));
                }
                new Output("1225", "Recuperado Eventos");
            }
            else
            {
                new Output("1250", "Sem eventos no calendario");
            }
            Console.Read();

            return MSEvents;
        }
        //recupera ID google a partir de uma calendario microsoft
        public String getGoogleID(Microsoft.Graph.Calendar calendar)
        {
            if (idgoogle != null && idgoogleName != null)
            {
                return idgoogle[idgoogleName.IndexOf(calendar.Name)];
            }
            return "";
        }
        /* 
         ********************************************************************************************************
         AREA DE EMAIL
         ********************************************************************************************************
         */
        //recupera o nome dos calendarios do google para a 
        public List<string> getMailBoxes()
        {
            UsersResource.LabelsResource.ListRequest request = mailservice.Users.Labels.List("me");
            ListLabelsResponse response = request.Execute();
            List<String> labels = new List<String>();
            foreach (Label la in response.Labels) {
                if(!la.Name.Equals("IMPORTANT") && !la.Name.Equals("CATEGORY_FORUMS") && !la.Name.Equals("CATEGORY_PERSONAL") && !la.Name.Equals("CATEGORY_PROMOTIONS") && !la.Name.Equals("CATEGORY_SOCIAL") && !la.Name.Equals("CATEGORY_UPDATES") && !la.Name.Equals("UNREAD"))
                    labels.Add(la.Name);
            }
            return labels;
        }
        public async Task getMailAsync(String box, String folderID, MsGraph currentGraph)
        {
            UsersResource.MessagesResource.ListRequest request = mailservice.Users.Messages.List("me");
            request.LabelIds = box;
            do
            {
                try
                {
                    var result = request.Execute();
                    IList<Message> mesages = result.Messages;
                    new Output("2220", "Procurando mensagens em " + request.LabelIds);
                    foreach (var email in mesages)
                    {
                        try
                        {
                            var emailInfoRequest = mailservice.Users.Messages.Get(request.UserId, email.Id);
                            // Make another request for that email id...
                            var emailInfoResponse = emailInfoRequest.Execute();
                            Microsoft.Graph.Message messageMS = this.convertMessage(emailInfoResponse, folderID);
                            Microsoft.Graph.Message ms = await MigrateGmailAPI.cloudUpMessageAsync(currentGraph, messageMS, folderID);

                        }
                        catch (Exception e)
                        {
                            Console.Out.WriteLine(e.Message);
                        }
                    }
                    request.PageToken = result.NextPageToken;
                }
                catch (Exception e)
                {
                    new Output("2250", "Erro ao recuperar proxima pagina");
                }

            } while (!String.IsNullOrEmpty(request.PageToken));
            new Output("2229", "Finalizado caixa" + box);
        }
        public  Microsoft.Graph.Message convertMessage(Message messages, string box)
        { 
            IList<Microsoft.Graph.Message> converted = new List<Microsoft.Graph.Message>();
                Microsoft.Graph.Message MSmessage = new Microsoft.Graph.Message();
            MSmessage.SingleValueExtendedProperties = new Microsoft.Graph.MessageSingleValueExtendedPropertiesCollectionPage();
            foreach (var mParts in messages.Payload.Headers)
            {
                
                if (mParts.Name == "Date")
                {
                    string data = mParts.Value;
                    try { data = data.Replace(data.Substring(data.IndexOf('(')), "").Trim(); } catch { }
                    data = data.Replace(",", "").Trim();
                    DateTimeOffset date;
                    try
                    {
                         date = DateTimeOffset.ParseExact(data,
                                "ddd dd MMM yyyy HH:mm:ss K", CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                         date = DateTimeOffset.ParseExact(data,
                            "ddd d MMM yyyy HH:mm:ss K", CultureInfo.InvariantCulture);
                    }
                    MSmessage.SentDateTime = date;
                    MSmessage.ReceivedDateTime = date;
                    MSmessage.CreatedDateTime = date;
                    MSmessage.SingleValueExtendedProperties.Add(new Microsoft.Graph.SingleValueLegacyExtendedProperty
                    {
                        Id = "SystemTime 0x0039",
                        Value = date.Year.ToString("00") + "-"+ date.Month.ToString("00") + "-" + date.Day.ToString("00") + "T" + date.Hour.ToString("00") + ":" + date.Minute.ToString("00") + ":" + date.Second.ToString("00") + "." + date.Millisecond.ToString("0000")  + data.Substring(data.IndexOf('+'))
                    });
                    MSmessage.SingleValueExtendedProperties.Add(new Microsoft.Graph.SingleValueLegacyExtendedProperty
                    {
                        Id = "SystemTime 0x0E06",
                        Value = date.Year.ToString("00") + "-" + date.Month.ToString("00") + "-" + date.Day.ToString("00") + "T" + date.Hour.ToString("00") + ":" + date.Minute.ToString("00") + ":" + date.Second.ToString("00") +"." + date.Millisecond.ToString("0000") + data.Substring(data.IndexOf('+'))
                    });

                }
                else if (mParts.Name == "From")
                {
                    Microsoft.Graph.Recipient from = null;
                    foreach (var recipent in mParts.Value.Split(';'))
                    {
                        try
                        {
                            string[] splits = recipent.Split('<');
                            splits[1] = splits[1].Replace(">", "").Replace("\\\"", "").Trim();
                            from =new Microsoft.Graph.Recipient
                            {

                                EmailAddress = new Microsoft.Graph.EmailAddress
                                {
                                    Name = splits[0],
                                    Address = splits[1]
                                }
                            };
                        }
                        catch
                        {
                            from = new Microsoft.Graph.Recipient
                            {

                                EmailAddress = new Microsoft.Graph.EmailAddress
                                {
                                    Address = recipent
                                }
                            };
                        }
                    }
                    MSmessage.From = from;
                }
                else if (mParts.Name == "To")
                {
                    List<Microsoft.Graph.Recipient> to = new List<Microsoft.Graph.Recipient>();
                    foreach (var recipent in mParts.Value.Split(';'))
                    {
                        try
                        {
                            string[] splits = recipent.Split('<');
                            splits[1] = splits[1].Replace(">", "").Replace("\\\"", "").Trim();
                            to.Add(new Microsoft.Graph.Recipient
                            {

                                EmailAddress = new Microsoft.Graph.EmailAddress
                                {
                                    Name = splits[0],
                                    Address = splits[1]
                                }
                            });
                        }
                        catch
                        {
                            to.Add(new Microsoft.Graph.Recipient
                            {

                                EmailAddress = new Microsoft.Graph.EmailAddress
                                {
                                    Address = recipent
                                }
                            });
                        }
                    }
                    MSmessage.ToRecipients = to;
                }
                else if (mParts.Name == "Subject")
                {
                    MSmessage.Subject = mParts.Value;
                }
                
            }
            string body;
            if (messages.Payload.Parts == null && messages.Payload.Body != null)
            {
                body = Encoding.UTF8.GetString(base64(messages.Payload.Body.Data));
            }
            else
            {
                body = getNestedPartsAsync(messages.Payload.Parts, "", messages.Id, MSmessage);
            }
            // Need to replace some characters as the data for the email's body is base64
            MSmessage.Body = new Microsoft.Graph.ItemBody()
            {
                Content = body,
                ContentType = Microsoft.Graph.BodyType.Html
            };
            if (messages.LabelIds.Contains("UNREAD"))
                MSmessage.IsRead = true;
            else
                MSmessage.IsRead = false;

            if (messages.LabelIds.Contains("IMPORTANT"))
            {
                MSmessage.Importance = Microsoft.Graph.Importance.High;
            }
            if (!messages.LabelIds.Contains("DRAFT"))
            {
                MSmessage.SingleValueExtendedProperties.Add(new Microsoft.Graph.SingleValueLegacyExtendedProperty
                    {
                        Id = "Integer 0x0E07",
                        Value = "1"
                    });
            }
            
            
            MSmessage.ConversationId = messages.ThreadId;
            MSmessage.ParentFolderId = box;
            return MSmessage;
        }
        string getNestedPartsAsync(IList<MessagePart> part, string curr, string id, Microsoft.Graph.Message messag)
        {
            string str = curr;
            if (part != null)
            {
                var counter = 0;
                foreach (var parts in part)
                {
                    
                    if (parts.Parts == null)
                    {
                        if (!string.IsNullOrEmpty(parts.Filename) && !parts.Filename.Contains("messagegoogle") && !parts.Filename.Contains("invite.ics"))
                        {
                            String filename = parts.Filename;
                            String attId = parts.Body.AttachmentId;
                            MessagePartBody attachPart = mailservice.Users.Messages.Attachments
                                .Get("me", id, attId).Execute();
                            byte[] fileByteArray = base64(attachPart.Data);
                            if(messag.HasAttachments == null)
                            {
                                messag.Attachments = new Microsoft.Graph.MessageAttachmentsCollectionPage();
                                messag.HasAttachments = true;
                            }
                            Microsoft.Graph.FileAttachment attachment = new Microsoft.Graph.FileAttachment()
                            {
                                Name = filename,
                                ContentBytes = fileByteArray,
                                ContentType = parts.MimeType,
                            };
                            Regex reg = new Regex("\\[cid:" + filename + "(.*?)\\]", RegexOptions.IgnoreCase );
                            if (reg.IsMatch(str))
                            {
                                str = reg.Replace(str, "");
                                reg = new Regex("\"cid:" + filename + "(.*?)\"", RegexOptions.IgnoreCase);
                            }
                            if ((filename.ToLower().Contains("jpg") || filename.ToLower().Contains("png") || filename.ToLower().Contains("bmp") || filename.ToLower().Contains("gif")) && reg.IsMatch(str))
                            {
                                str = reg.Replace(str, ("\"cid:" + filename + "\""));
                                attachment.IsInline = true;
                            }
                            messag.Attachments.Add(attachment);
                            
                        }
                        else
                        if ( parts.Body != null && parts.Body.Data != null )
                        {
                            var teste = true;
                            if(parts.Headers != null)
                                foreach (var header in parts.Headers)
                                    if (header.Value.Contains("text/plain"))
                                        teste = false;
                            if(teste)
                                str += Encoding.UTF8.GetString(base64(parts.Body.Data));
                        }
                        counter++;
                        Console.WriteLine(counter);
                    }
                    else
                    {
                        str += getNestedPartsAsync(parts.Parts, str, id, messag);
                    }
                }
            }
            return str;
        }
        static private byte[] base64(String codedBody)
        {
            codedBody = codedBody.Replace("-", "+");
            codedBody = codedBody.Replace("_", "/");
            return Convert.FromBase64String(codedBody);
        }
        /* 
         ********************************************************************************************************
         AREA DE CONTATOS
         ********************************************************************************************************
         */
        public async Task contacsAsync(MsGraph currentGraph)
        {
            IList<Microsoft.Graph.Contact> contacts = new List<Microsoft.Graph.Contact>();
            PeopleResource.ConnectionsResource.ListRequest peopleRequest =
                contactsService.People.Connections.List("people/me");
            peopleRequest.PersonFields = maskContacts;
            ListConnectionsResponse connectionsResponse = peopleRequest.Execute();
            IList<Person> connections = connectionsResponse.Connections;
            foreach (Person person in connections)
            {
                Microsoft.Graph.Contact contact = new Microsoft.Graph.Contact();
                if (person.Names != null && person.Names.Count > 0) {
                    contact.GivenName = person.Names[0].GivenName;
                    contact.DisplayName = person.Names[0].DisplayName;
                    contact.MiddleName =person.Names[0].MiddleName;
                    contact.GivenName = person.Names[0].GivenName;
                    contact.Surname = person.Names[0].FamilyName;
                }
                if (person.Birthdays != null && person.Birthdays.Count > 0)
                    try
                    {
                        contact.Birthday = DateTimeOffset.ParseExact(person.Birthdays[0].Text,
                                    "dd,M,yyyy", CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        try
                        {
                            contact.Birthday = DateTimeOffset.ParseExact(person.Birthdays[0].Text,
                                    "d,M,yyyy", CultureInfo.InvariantCulture);
                        }
                        catch
                        {
                            try {
                                contact.Birthday = DateTimeOffset.ParseExact(person.Birthdays[0].Text,
                                  "d,MM,yyyy", CultureInfo.InvariantCulture);
                            }
                            catch
                            {
                                try
                                {
                                    contact.Birthday = DateTimeOffset.ParseExact(person.Birthdays[0].Text,
                                          "dd,MM,yyyy", CultureInfo.InvariantCulture);
                                }
                                catch
                                {

                                }
                            }
                        }
                    }

                if (person.Addresses != null && person.Addresses.Count > 0)
                {
                    contact.BusinessAddress = new Microsoft.Graph.PhysicalAddress();
                    contact.BusinessAddress.City = person.Addresses[0].City;
                    contact.BusinessAddress.PostalCode = person.Addresses[0].PostalCode;
                    contact.BusinessAddress.CountryOrRegion = person.Addresses[0].Country;
                    contact.BusinessAddress.State = person.Addresses[0].Region;
                    contact.BusinessAddress.Street = person.Addresses[0].StreetAddress;
                    if (person.Addresses.Count > 1)
                    {
                        contact.HomeAddress = new Microsoft.Graph.PhysicalAddress();
                        contact.HomeAddress.City = person.Addresses[1].City;
                        contact.HomeAddress.PostalCode = person.Addresses[1].PostalCode;
                        contact.HomeAddress.CountryOrRegion = person.Addresses[1].Country;
                        contact.HomeAddress.State = person.Addresses[1].Region;
                        contact.HomeAddress.Street = person.Addresses[1].StreetAddress;

                        if (person.Addresses.Count > 2)
                        {
                            contact.OtherAddress = new Microsoft.Graph.PhysicalAddress();
                            contact.OtherAddress.City = person.Addresses[2].City;
                            contact.OtherAddress.PostalCode = person.Addresses[2].PostalCode;
                            contact.OtherAddress.CountryOrRegion = person.Addresses[2].Country;
                            contact.OtherAddress.State = person.Addresses[2].Region;
                            contact.OtherAddress.Street = person.Addresses[2].StreetAddress;

                        }
                    }
                }
                if (person.Nicknames != null && person.Nicknames.Count > 0)
                    contact.NickName = person.Nicknames[0].Value;
                if (person.Organizations != null && person.Organizations.Count > 0)
                {
                    contact.Department = person.Organizations[0].Department;
                    contact.CompanyName = person.Organizations[0].Name;
                    contact.JobTitle = person.Organizations[0].Title;
                    contact.OfficeLocation = person.Organizations[0].Location;
                }
                if (person.PhoneNumbers != null && person.PhoneNumbers.Count > 0)
                {
                    List<String> BusinessPhones = new List<String>();
                    List<String> HomePhones = new List<String>();
                    var count = 0;
                    foreach (var phone in person.PhoneNumbers) {
                        if (BusinessPhones.Count<2 && phone.Type.Contains("work")) { BusinessPhones.Add(phone.Value); }
                        if (String.IsNullOrEmpty(contact.MobilePhone) && phone.Type.Contains("mobile")) { contact.MobilePhone=phone.Value; }
                        if (HomePhones.Count < 2 && phone.Type.Contains("home")) { HomePhones.Add(phone.Value); }
                    }
                    contact.BusinessPhones = BusinessPhones;
                    contact.HomePhones = HomePhones;
                }
                if (person.Urls != null && person.Urls.Count > 0)
                    contact.BusinessHomePage = person.Urls[0].Value;
                if (person.EmailAddresses != null && person.EmailAddresses.Count > 0)
                {
                    List<Microsoft.Graph.EmailAddress> mail = new List<Microsoft.Graph.EmailAddress>();
                    foreach (var email in person.EmailAddresses)
                    {
                        mail.Add(new Microsoft.Graph.EmailAddress()
                        {
                            Name = email.DisplayName,
                            Address = email.Value
                        });
                    }
                    contact.EmailAddresses = mail;
                }
                if (person.Taglines != null && person.Taglines.Count > 0)
                    contact.PersonalNotes = person.Taglines[0].Value;
                await MigrateContacts.migrateAsync(currentGraph,contact);
            }
        }

    }
}
