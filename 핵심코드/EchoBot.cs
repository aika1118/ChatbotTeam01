// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.9.2

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System.Text;
using System.Net.Http;
using Microsoft.Extensions.Configuration;

namespace EchoBot1.Bots
{
    public class EchoBot : ActivityHandler
    {
        //  private QnAMakerEndpoint endpoint;



        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            await GetAnswerFromQnAMaker(turnContext, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var welcomeText = "안녕하세요, 현명한집사팀 입니다. 우리는 반려동물의 종합서비스를 위한 동물병원 예약, 간식구매 기능과 반려동물의 행복도를 높이기 위해 주인이 알아야할 정보를 제공하는 기능을 만들고 있습니다.";
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
                }
            }
        }
        private async Task GetAnswerFromQnAMaker(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var qnaOptions = new QnAMakerOptions();
            qnaOptions.ScoreThreshold = 0.1F;

            // The actual call to the QnA Maker service.
            var response = await EchoBotQnA.GetAnswersAsync(turnContext, qnaOptions);

            if (response != null && response.Length > 0)
            {
                // create http client to perform qna query
                var followUpCheckHttpClient = new HttpClient();

                // add QnAAuthKey to Authorization header
                followUpCheckHttpClient.DefaultRequestHeaders.Add("Authorization", "68fd4412-af8f-472c-9255-59e5455cd519");

                // construct the qna query url
                var url = $"{"https://rg-team01.azurewebsites.net/qnamaker"}/knowledgebases/{"d59fd8fe-4708-41f8-9e78-1f4a0e4712e2"}/generateAnswer";

                // post query
                var checkFollowUpJsonResponse = await followUpCheckHttpClient.PostAsync(url, new StringContent("{\"question\":\"" + turnContext.Activity.Text + "\"}", Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();

                // parse result
                var followUpCheckResult = JsonConvert.DeserializeObject<FollowUpCheckResult>(checkFollowUpJsonResponse);

                // initialize reply message containing the default answer
                var reply = MessageFactory.Text(response[0].Answer);

                if (followUpCheckResult.Answers.Length > 0 && followUpCheckResult.Answers[0].Context.Prompts.Length > 0)
                {
                    // if follow-up check contains valid answer and at least one prompt, add prompt text to SuggestedActions using CardAction one by one
                    reply.SuggestedActions = new SuggestedActions();
                    reply.SuggestedActions.Actions = new List<CardAction>();
                    for (int i = 0; i < followUpCheckResult.Answers[0].Context.Prompts.Length; i++)
                    {
                        var promptText = followUpCheckResult.Answers[0].Context.Prompts[i].DisplayText;
                        reply.SuggestedActions.Actions.Add(new CardAction() { Title = promptText, Type = ActionTypes.ImBack, Value = promptText });
                    }
                }
                await turnContext.SendActivityAsync(reply, cancellationToken);
            }
            else
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("No QnA Maker answers were found."), cancellationToken);
            }
        }

        public QnAMaker EchoBotQnA { get; private set; }

        public EchoBot(QnAMakerEndpoint endpoint)
        {
            EchoBotQnA = new QnAMaker(endpoint);
        }

        class FollowUpCheckResult
        {
            [JsonProperty("answers")]
            public FollowUpCheckQnAAnswer[] Answers
            {
                get;
                set;
            }
        }

        class FollowUpCheckQnAAnswer
        {
            [JsonProperty("context")]
            public FollowUpCheckContext Context
            {
                get;
                set;
            }
        }

        class FollowUpCheckContext
        {
            [JsonProperty("prompts")]
            public FollowUpCheckPrompt[] Prompts
            {
                get;
                set;
            }
        }

        class FollowUpCheckPrompt
        {
            [JsonProperty("displayText")]
            public string DisplayText
            {
                get;
                set;
            }
        }
    }
}
