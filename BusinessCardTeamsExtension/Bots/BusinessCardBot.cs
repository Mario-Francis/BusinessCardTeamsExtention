using AdaptiveCards;
using BusinessCardTeamsExtension.DTOs;
using BusinessCardTeamsExtension.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessCardTeamsExtension.Bots
{
    public class BusinessCardBot : TeamsActivityHandler
    {
        private readonly IBusinessCardService businessCardService;
        private readonly ILogger<BusinessCardBot> logger;
        private readonly IMicrosoftGraphService microsoftGraphService;
        private readonly IWebHostEnvironment env;
        private readonly UserState userState;

        public BusinessCardBot(IBusinessCardService businessCardService, 
            ILogger<BusinessCardBot> logger, 
            IMicrosoftGraphService microsoftGraphService, 
            IWebHostEnvironment env,
            UserState userState)
        {
            this.businessCardService = businessCardService;
            this.logger = logger;
            this.microsoftGraphService = microsoftGraphService;
            this.env = env;
            this.userState = userState;
        }
        protected override async Task<MessagingExtensionActionResponse> OnTeamsMessagingExtensionSubmitActionAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionAction action, CancellationToken cancellationToken)
        {
            if (action.CommandId == "shareBusinessCard")
            {
                return await GetBusinessCardResponse(turnContext, action, cancellationToken);
            }
            else if (action.CommandId == "viewContacts")
            {
                return await ViewContactsResponse(turnContext, action, cancellationToken);
            }
            else
            {
                var errorResponse = GetErrorResponse("Invalid Command", $"Command '{action.CommandId}' is not recognised. Kindly report this to your system administrator");
                return errorResponse;
            }
        }

        private async Task<MessagingExtensionActionResponse> ViewContactsResponse(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionAction action, CancellationToken cancellationToken)
        {
            try
            {
                var member = await microsoftGraphService.GetUser(turnContext.Activity.From.AadObjectId);
                var email = "sarfraz@mycards.com";
                if (!env.IsDevelopment())
                {
                    email = member.Email;
                }
                var userIdResponse = await businessCardService.GetUserId(email);
                if (!userIdResponse.IsSuccess)
                {
                    return GetErrorResponse("Error encountered on user id fetch", userIdResponse.Message);
                }
                else
                {
                    var userId = "V2ExeEhNQktKU2g0TjFOK3pNeXAvZz09";
                    if (!env.IsDevelopment())
                    {
                        userId = userIdResponse.UserId;
                    }
                    var cardResponse = await businessCardService.GetUserBusinessCard(userId);
                    if (!cardResponse.IsSuccess)
                    {
                        return GetErrorResponse("Error encountered on business card fetch", cardResponse.Message);
                    }
                    else
                    {
                        var name = string.IsNullOrEmpty(member.GivenName) || string.IsNullOrEmpty(member.Surname) ? member.Email : $"{member.GivenName} {member.Surname}";

                        AdaptiveCard card = new AdaptiveCard("1.0");
                        card.Body.Add(new AdaptiveTextBlock
                        {
                            Text = "Click on the button below to view your contacts",
                            Size = AdaptiveTextSize.Medium,
                            Weight = AdaptiveTextWeight.Default,
                            Wrap = true,
                            MaxLines = 2
                        });
                        card.Actions.Add(new AdaptiveOpenUrlAction
                        {
                            Title = "View Contacts",
                            Url =  new Uri(cardResponse.MyContactsUrl)
                        });
                       
                        Attachment attachment = new Attachment()
                        {
                            ContentType = AdaptiveCard.ContentType,
                            Content = card
                        };
                        return new MessagingExtensionActionResponse
                        {
                            Task = new TaskModuleContinueResponse
                            {
                                Value = new TaskModuleTaskInfo
                                {
                                    Card = attachment,
                                    Height = 150,
                                    Width = 300,
                                    Title = "myContacts",
                                },
                            },
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return GetErrorResponse("Something went wrong", ex.Message);
            }
        }

        protected async Task<MessagingExtensionActionResponse> GetBusinessCardResponse(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionAction action, CancellationToken cancellationToken)
        {
            try
            {
                //var member = await TeamsInfo.GetMemberAsync(turnContext, turnContext.Activity.From.Id, cancellationToken);
                var member = await microsoftGraphService.GetUser(turnContext.Activity.From.AadObjectId);
                var email = "sarfraz@mycards.com";
                if (!env.IsDevelopment())
                {
                    email = member.Email;
                }
                var userIdResponse = await businessCardService.GetUserId(email);
                if (!userIdResponse.IsSuccess)
                {
                    return GetErrorResponse("Error encountered on user id fetch", userIdResponse.Message);
                }
                else
                {
                    var userId = "V2ExeEhNQktKU2g0TjFOK3pNeXAvZz09";
                    if (!env.IsDevelopment())
                    {
                        userId = userIdResponse.UserId;
                    }
                    var cardResponse = await businessCardService.GetUserBusinessCard(userId);
                    if (!cardResponse.IsSuccess)
                    {
                        return GetErrorResponse("Error encountered on business card fetch", cardResponse.Message);
                    }
                    else
                    {
                        var name = string.IsNullOrEmpty(member.GivenName) || string.IsNullOrEmpty(member.Surname) ? member.Email : $"{member.GivenName} {member.Surname}";
                        var cardUrl = cardResponse.UrlWithoutMobile;
                        var withMobile = ((JObject)action.Data)["withMobile"]?.ToString();
                        if (string.Equals(withMobile, bool.TrueString, StringComparison.OrdinalIgnoreCase))
                        {
                            cardUrl = cardResponse.Url;
                        }

                        var card = new HeroCard
                        {
                            Title = $"{name}'s Business Card",
                            Text = "Click the button below to view my business card.",
                            Buttons = new List<CardAction> {
                                new CardAction(ActionTypes.OpenUrl,"View Card", text: "View card", value:cardUrl)
                            },
                        };

                        var attachments = new List<MessagingExtensionAttachment>();
                        attachments.Add(new MessagingExtensionAttachment
                        {
                            Content = card,
                            ContentType = HeroCard.ContentType,
                            Preview = card.ToAttachment(),
                        });
                        var response = new MessagingExtensionActionResponse
                        {
                            ComposeExtension = new MessagingExtensionResult
                            {
                                AttachmentLayout = "list",
                                Type = "result",
                                Attachments = attachments,
                            },
                        };
                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return GetErrorResponse("Something went wrong", ex.Message);
            }
        }

        private MessagingExtensionActionResponse GetErrorResponse(string title, string message)
        {
            AdaptiveCard card = new AdaptiveCard("1.0");
            card.Body.Add(new AdaptiveTextBlock
            {
                Text = title,
                Size = AdaptiveTextSize.Medium,
                Weight = AdaptiveTextWeight.Bolder,
                Wrap = true,
                MaxLines = 2
            });
            card.Body.Add(new AdaptiveRichTextBlock
            {
                Inlines = new List<AdaptiveInline>()
                {
                    new AdaptiveTextRun{Text=$"Message: ", Weight=AdaptiveTextWeight.Bolder},
                    new AdaptiveTextRun{Text=$"{message}.", Weight=AdaptiveTextWeight.Default},
                },
            });
            Attachment attachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };
            return new MessagingExtensionActionResponse
            {
                Task = new TaskModuleContinueResponse
                {
                    Value = new TaskModuleTaskInfo
                    {
                        Card = attachment,
                        Height = 150,
                        Width = 300,
                        Title = "Busiess Card Extension Error",
                    },
                },
            };
        }


        //===========================
        //protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        //{
        //    //return base.OnMessageActivityAsync(turnContext, cancellationToken);
        //    await turnContext.SendActivityAsync(MessageFactory.Text($"Hello and welcome!"), cancellationToken);
        //}

        //protected override Task OnConversationUpdateActivityAsync(ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        //{
        //    //turnContext.Activity.
        //    return base.OnConversationUpdateActivityAsync(turnContext, cancellationToken);
        //}

        //protected override Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        //{
        //    return base.OnMembersAddedAsync(membersAdded, turnContext, cancellationToken);
        //}

        private const string WelcomeMessage = "This is a simple Welcome Bot sample. This bot will introduce you " +
                                                "to welcoming and greeting users. You can say 'intro' to see the " +
                                                "introduction card. If you are running this bot in the Bot Framework " +
                                                "Emulator, press the 'Start Over' button to simulate user joining " +
                                                "a bot or a channel";

        private const string InfoMessage = "You are seeing this message because the bot received at least one " +
                                            "'ConversationUpdate' event, indicating you (and possibly others) " +
                                            "joined the conversation. If you are using the emulator, pressing " +
                                            "the 'Start Over' button to trigger this event again. The specifics " +
                                            "of the 'ConversationUpdate' event depends on the channel. You can " +
                                            "read more information at: " +
                                            "https://aka.ms/about-botframework-welcome-user";

        private const string LocaleMessage = "You can use the activity's 'GetLocale()' method to welcome the user " +
                                             "using the locale received from the channel. " +
                                             "If you are using the Emulator, you can set this value in Settings.";


        private const string PatternMessage = "It is a good pattern to use this event to send general greeting" +
                                              "to user, explaining what your bot can do. In this example, the bot " +
                                              "handles 'hello', 'hi', 'help' and 'intro'. Try it now, type 'hi'";



        // Greet when users are added to the conversation.
        // Note that all channels do not send the conversation update activity.
        // If you find that this bot works in the emulator, but does not in
        // another channel the reason is most likely that the channel does not
        // send this activity.
        //protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        //{
        //    foreach (var member in membersAdded)
        //    {
        //        if (member.Id != turnContext.Activity.Recipient.Id)
        //        {
        //            await turnContext.SendActivityAsync($"Hi there - {member.Name}. {WelcomeMessage}", cancellationToken: cancellationToken);
        //            await turnContext.SendActivityAsync(InfoMessage, cancellationToken: cancellationToken);
        //            await turnContext.SendActivityAsync($"{LocaleMessage} Current locale is '{turnContext.Activity.GetLocale()}'.", cancellationToken: cancellationToken);
        //            await turnContext.SendActivityAsync(PatternMessage, cancellationToken: cancellationToken);
        //        }
        //    }
        //}

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync($"Welcome {member.Name}!", cancellationToken: cancellationToken);
                    
                }
            }
            await SendMyCardsIntroAsync(turnContext, cancellationToken);
        }

        //protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        //{
        //    var welcomeUserStateAccessor = userState.CreateProperty<WelcomeUserState>(nameof(WelcomeUserState));
        //    var didBotWelcomeUser = await welcomeUserStateAccessor.GetAsync(turnContext, () => new WelcomeUserState(), cancellationToken);

        //    if (didBotWelcomeUser.DidBotWelcomeUser == false)
        //    {
        //        didBotWelcomeUser.DidBotWelcomeUser = true;

        //        // the channel should sends the user name in the 'From' object
        //        var userName = turnContext.Activity.From.Name;

        //        await turnContext.SendActivityAsync("You are seeing this message because this was your first message ever to this bot.", cancellationToken: cancellationToken);
        //        await turnContext.SendActivityAsync($"It is a good practice to welcome the user and provide personal greeting. For example, welcome {userName}.", cancellationToken: cancellationToken);
        //    }
        //    else
        //    {
        //        // This example hardcodes specific utterances. You should use LUIS or QnA for more advance language understanding.
        //        var text = turnContext.Activity.Text.ToLowerInvariant();
        //        switch (text)
        //        {
        //            case "hello":
        //            case "hi":
        //                await turnContext.SendActivityAsync($"You said {text}.", cancellationToken: cancellationToken);
        //                break;
        //            case "intro":
        //            case "help":
        //                await SendIntroCardAsync(turnContext, cancellationToken);
        //                break;
        //            default:
        //                await turnContext.SendActivityAsync(WelcomeMessage, cancellationToken: cancellationToken);
        //                break;
        //        }
        //    }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var welcomeUserStateAccessor = userState.CreateProperty<WelcomeUserState>(nameof(WelcomeUserState));
            var didBotWelcomeUser = await welcomeUserStateAccessor.GetAsync(turnContext, () => new WelcomeUserState(), cancellationToken);

            if (didBotWelcomeUser.DidBotWelcomeUser == false)
            {
                didBotWelcomeUser.DidBotWelcomeUser = true;

                // the channel should sends the user name in the 'From' object
                var userName = turnContext.Activity.From.Name;

                await SendMyCardsIntroAsync(turnContext, cancellationToken);
            }
            else
            {
                // This example hardcodes specific utterances. You should use LUIS or QnA for more advance language understanding.
                var text = turnContext.Activity.Text.ToLowerInvariant();
                switch (text)
                {
                    case "hello":
                    case "hi":
                    case "intro":
                    case "help":
                    default:
                        await SendMyCardsIntroAsync(turnContext, cancellationToken);
                        break;
                }
            }

            // Save any state changes.
            await userState.SaveChangesAsync(turnContext, cancellationToken: cancellationToken);
        }

        private static async Task SendIntroCardAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var card = new HeroCard
            {
                Title = "Welcome to Bot Framework!",
                Text = @"Welcome to Welcome Users bot sample! This Introduction card
is a great way to introduce your Bot to the user and suggest
some things to get them started. We use this opportunity to
recommend a few next steps for learning more creating and deploying bots.",
                Images = new List<CardImage>() { new CardImage("https://aka.ms/bf-welcome-card-image") },
                Buttons = new List<CardAction>()
                {
                    new CardAction(ActionTypes.OpenUrl, "Get an overview", null, "Get an overview", "Get an overview", "https://docs.microsoft.com/en-us/azure/bot-service/?view=azure-bot-service-4.0"),
                    new CardAction(ActionTypes.OpenUrl, "Ask a question", null, "Ask a question", "Ask a question", "https://stackoverflow.com/questions/tagged/botframework"),
                    new CardAction(ActionTypes.OpenUrl, "Learn how to deploy", null, "Learn how to deploy", "Learn how to deploy", "https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-howto-deploy-azure?view=azure-bot-service-4.0"),
                }
            };

            var response = MessageFactory.Attachment(card.ToAttachment());
            await turnContext.SendActivityAsync(response, cancellationToken);
        }

        private static async Task SendMyCardsIntroAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var card = new HeroCard
            {
                Title = "myCards Chat Bot",
                Text = @"Hello! I’m myCards chat bot and on behalf of the myCards Team, thanks for taking another step to saving our trees one business card at a time!
 <br /><br />
To share your business card: <br />
<ul>
<li>Share via Microsoft Teams meeting chat window with ALL attendees: Click on myCards icon at the bottom of the chat window (compose message area) then select <b>Share myCard</b>.</li>
<li style='margin-top:10px;'>Share via Microsoft Teams meeting with individual attendees: Click on the individuals profile image or initials, select <b>Start a chat</b>, select the myCards icon, click <b>Share myCard.</b></li>
</ul>
  <br />
To access your received business cards, sign into the myCards portal by clicking the button below
<br /><br />
",
                //Images = new List<CardImage>() { new CardImage("https://aka.ms/bf-welcome-card-image") },
                Buttons = new List<CardAction>()
                {
                    new CardAction(ActionTypes.OpenUrl, "Sign In", null, "Sign In", "Sign In", "https://webapp.mycards.com"),
                    //new CardAction(ActionTypes.OpenUrl, "Ask a question", null, "Ask a question", "Ask a question", "https://stackoverflow.com/questions/tagged/botframework"),
                    //new CardAction(ActionTypes.OpenUrl, "Learn how to deploy", null, "Learn how to deploy", "Learn how to deploy", "https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-howto-deploy-azure?view=azure-bot-service-4.0"),
                }
            };

            var response = MessageFactory.Attachment(card.ToAttachment());
            await turnContext.SendActivityAsync(response, cancellationToken);
        }
        //==========================
    }
}
