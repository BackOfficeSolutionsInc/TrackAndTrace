using OpenPop.Mime;
using OpenPop.Pop3;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities
{
    public class Pop3Utility
    {

        public List<String> FindUnsentEmails()
        {
            return null;
        }

        public static void FindPlainTextInMessage(Message message)
        {
            MessagePart plainText = message.FindFirstPlainTextVersion();
            if (plainText != null)
            {
                // Save the plain text to a file, database or anything you like
                plainText.Save(new FileInfo("plainText.txt"));
            }
        }

        public static List<Message> FetchAllMessages(string hostname, int port, bool useSsl, string username, string password)
        {
            // The client disconnects from the server when being disposed
            using (Pop3Client client = new Pop3Client())
            {
                // Connect to the server
                client.Connect(ConstantStrings.PopHost, ConstantStrings.PopPort.ToInt(), true);

                // Authenticate ourselves towards the server
                client.Authenticate(ConstantStrings.PopLogin, ConstantStrings.PopPassword);

                // Get the number of messages in the inbox
                int messageCount = client.GetMessageCount();

                // We want to download all messages
                List<Message> allMessages = new List<Message>(messageCount);

                // Messages are numbered in the interval: [1, messageCount]
                // Ergo: message numbers are 1-based.
                // Most servers give the latest message the highest number
                for (int i = messageCount; i > 0; i--)
                {
                    allMessages.Add(client.GetMessage(i));
                }

                // Now return the fetched messages
                return allMessages;
            }
        }
    }
}