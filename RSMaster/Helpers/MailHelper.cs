using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MailKit.Search;
using MailKit;
using MailKit.Net.Imap;

namespace RSMaster.Helpers
{
    using MimeKit;
    using Objects;

    internal enum MailProvider
    {
        Gmail,
        Yahoo,
        Outlook
    }

    internal class MailHelper
    {
        private readonly ImapClient client;
        private readonly ImapDetails provider;
        private readonly Dictionary<MailProvider, ImapDetails> providerDetails = new Dictionary<MailProvider, ImapDetails>()
        {
            { MailProvider.Gmail, new ImapDetails { Host = "imap.gmail.com" } },
            { MailProvider.Yahoo, new ImapDetails { Host = "imap.mail.yahoo.com"} }
        };

        public MailHelper(MailProvider mailProvider)
        {
            client = new ImapClient();
            provider = providerDetails[mailProvider];
        }

        public bool Connect(string username, string password)
        {
            if (client != null)
            {
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                client.Connect(provider.Host, provider.Port, provider.UseSsl);
                client.Authenticate(username, password);

                return client.IsConnected;
            }

            return false;
        }

        public void Disconnect()
        {
            if (client != null)
            {
                client.Disconnect(true);
            }
        }

        public string FindMailBySubjToHtml(string subject, DateTime after)
        {
            var query = SearchQuery.SubjectContains(subject);

            return FindMailsByQuery(query).FirstOrDefault(x => x.Date > after)?.HtmlBody ?? string.Empty;
        }

        private IEnumerable<MimeMessage> FindMailsByQuery(TextSearchQuery query)
        {
            var inbox = client.Inbox;
            if (!inbox.IsOpen)
            {
                inbox.Open(FolderAccess.ReadOnly);
            }

            return inbox.Search(query).Select(x => inbox.GetMessage(x));
        }
    }
}
