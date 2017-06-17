using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Builder.Azure;
using System.Configuration;
using ManagedMoochApi;
using System.Collections.Generic;
using ManagedMoochApi.Model;
using System.Linq;
using Microsoft.Bot.Builder.FormFlow;

namespace BotApp.Dialogs
{

    // For more information about this template visit http://aka.ms/azurebots-csharp-luis
    [Serializable]
    public class BasicLuisDialog : LuisDialog<object>
    {
        //protected string moochServerState;
        protected int currentEventIndex;
        protected MoochServer moochServerState;
        private const int EventsPerInteraction = 3;

        public BasicLuisDialog() : base(new LuisService(new LuisModelAttribute(ConfigurationManager.AppSettings["LuisAppId"], ConfigurationManager.AppSettings["LuisAPIKey"])))
        {
        }

        [LuisIntent("None")]
        public async Task NoneIntent(IDialogContext context, LuisResult result)
        {
            await context.PostAsync($"I'm sorry, I don't understand what you are asking. Ask me to find you events in your city");
            context.Wait(MessageReceived);
        }

        [LuisIntent("MoreEvents")]
        public async Task ShowMoreIntent(IDialogContext context, LuisResult result)
        {
            if (moochServerState == null)
            {
                await NoneIntent(context, result);
                return;
            }        
            else
            {
                await PostBackEvents(context);
            }
        }

        private async Task FindEventFormComplete(IDialogContext context, IAwaitable<FindEventForm> result)
        {
            FindEventForm form = null;
            try
            {
                form = await result;
            }
            catch (OperationCanceledException)
            {
            }

            string location = form.Location;
            await context.PostAsync($"Looking up some cool free events for you in the {location} area");

            moochServerState = new MoochServer(new MoochSettings()
            {
                ApiKey = "9ml",
                LocationFilter = location,
                BatchSize = 10
            });
            currentEventIndex = 0;

            await PostBackEvents(context);
        }

        private async Task PostBackEvents(IDialogContext context)
        {
            try
            {
                IEnumerable<MoochEvent> events = (await moochServerState.Events()).Skip(currentEventIndex).Take(EventsPerInteraction);
                if (events.Count() == 0)
                {
                    if (currentEventIndex == 0)
                    {
                        // If no events found, and there were no events shown before, we aren't in the area
                        await context.PostAsync($"Sorry, I'm not in your area yet. Currently most events are in the greater Seattle area.");
                    }
                    else
                    {
                        await context.PostAsync($"No more events found. I only show a few days at a time.");
                    }
                    return;
                }

                Activity replyToConversation = (Activity)context.MakeMessage();

                replyToConversation.Text = "Here are your events";
                replyToConversation.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                replyToConversation.Attachments = new List<Attachment>();

                foreach (MoochEvent moochEvent in events)
                {
                    this.currentEventIndex++;
                    List<CardAction> cardButtons = new List<CardAction>();

                    CardAction rsvpButton = new CardAction()
                    {
                        Value = moochEvent.DetailsUri,
                        Type = "openUrl",
                        Title = "Details & RSVP"
                    };
                    cardButtons.Add(rsvpButton);
                    
                    HeroCard plCard = new HeroCard()
                    {
                        Title = $"{moochEvent.Title}",
                        Subtitle = moochEvent.EventStart.ToString("dddd, MMM d, h:mm tt") + $" @ {moochEvent.Location.Name}",
                        Buttons = cardButtons,
                        Text = moochEvent.Description.Substring(0, Math.Min(250, moochEvent.Description.Length)) + "..."
                    };

                    Attachment plAttachment = plCard.ToAttachment();
                    replyToConversation.Attachments.Add(plAttachment);
                }

                if (events.Count() == EventsPerInteraction)
                {
                    replyToConversation.SuggestedActions = new SuggestedActions()
                    {
                        Actions = new List<CardAction>()
                        {
                            new CardAction()
                    {
                        Value = "Show me more events",
                        Type = "imBack",
                        Title = "Show me more events"
                    }
                        }
                    };
                    replyToConversation.InputHint = InputHints.AcceptingInput;
                }
                
                await context.PostAsync(replyToConversation);
            }
            finally
            {
                context.Wait(MessageReceived);
            }

        }

        [LuisIntent("Microsoft.Launch")]
        public async Task LaunchIntent(IDialogContext context, LuisResult result)
        {
            await context.PostAsync($"Hi! Ask me to find you events in your city. Currently I work best in the greater Seattle, WA area.");
            context.Wait(MessageReceived);

        }

        [LuisIntent("FindEvent")]
        public async Task FindEventIntent(IDialogContext context, LuisResult result)
        {
            var form = new FormDialog<FindEventForm>(
            new FindEventForm(),
            FindEventForm.BuildForm,
            FormOptions.PromptInStart,
            result.Entities);

            context.Call<FindEventForm>(form, FindEventFormComplete);

        }
    }

    [Serializable]
    public class FindEventForm
    {
        [Prompt("In what city? {||}", AllowDefault = BoolDefault.True)]
        [Describe("Tell me in what cities you want to find events in")]
        public string Location { get; set; }

        public static IForm<FindEventForm> BuildForm()
        {
            return new FormBuilder<FindEventForm>()
            .Field(nameof(Location))
            .Build();
        }
    }
}