using AdaptiveCards;
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

        public BusinessCardBot(IBusinessCardService businessCardService, ILogger<BusinessCardBot> logger, IMicrosoftGraphService microsoftGraphService, IWebHostEnvironment env)
        {
            this.businessCardService = businessCardService;
            this.logger = logger;
            this.microsoftGraphService = microsoftGraphService;
            this.env = env;
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
                var email = "AdamT@mycardsTeams.onmicrosoft.com";
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
                    var userId = "YncrZDhabE90aURzSnMzQXVUcGtmQT09";
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
                var email = "AdamT@mycardsTeams.onmicrosoft.com";
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
                    var userId = "YncrZDhabE90aURzSnMzQXVUcGtmQT09";
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
    }
}
