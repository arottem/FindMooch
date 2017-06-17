﻿using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Azure;
using BotApp.Dialogs;
using System;
using System.Linq;

namespace BotApp
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            try
            {
                // Initialize the azure bot
                using (BotService.Initialize())
                {
                    if (activity != null)
                    {
                        // one of these will have an interface and process it
                        switch (activity.GetActivityType())
                        {
                            case ActivityTypes.Message:
                                await Conversation.SendAsync(activity, () => new BasicLuisDialog());
                                break;
                            case ActivityTypes.ConversationUpdate:
                                var client = new ConnectorClient(new Uri(activity.ServiceUrl));
                                IConversationUpdateActivity update = activity;
                                if (update.MembersAdded.Any())
                                {
                                    var reply = activity.CreateReply();
                                    var newMembers = update.MembersAdded?.Where(t => t.Id != activity.Recipient.Id);
                                    foreach (var newMember in newMembers)
                                    {
                                        reply.Text = "Welcome";
                                        if (!string.IsNullOrEmpty(newMember.Name) && !newMember.Name.Equals("you", StringComparison.OrdinalIgnoreCase) && !newMember.Name.Equals("user", StringComparison.OrdinalIgnoreCase))
                                        {
                                            reply.Text += $" {newMember.Name}";
                                        }
                                        reply.Text += "!";
                                        await client.Conversations.ReplyToActivityAsync(reply);
                                    }
                                }
                                break;
                            case ActivityTypes.ContactRelationUpdate:
                            case ActivityTypes.Typing:
                            case ActivityTypes.DeleteUserData:
                            case ActivityTypes.Ping:
                            default:
                                //log.Error($"Unknown activity type ignored: {activity.GetActivityType()}");
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string error = ex.ToString();                

                //create response activity
                Activity response = activity.CreateReply(error);

                //post back to user
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                await connector.Conversations.ReplyToActivityAsync(response);
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex);
            }

            return Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}