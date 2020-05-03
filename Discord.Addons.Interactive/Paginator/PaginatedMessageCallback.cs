// ReSharper disable StyleCop.SA1503
namespace Discord.Addons.Interactive
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Discord.Commands;
    using Discord.Rest;
    using Discord.WebSocket;

    /// <summary>
    /// The paginated message callback.
    /// </summary>
    public class PaginatedMessageCallback : IReactionCallback
    {
        /// <summary>
        /// The run mode.
        /// </summary>
        public RunMode RunMode => RunMode.Sync;

        /// <summary>
        /// The timeout.
        /// </summary>
        public TimeSpan? Timeout => options.Timeout;

        /// <summary>
        /// The options.
        /// </summary>
        private PaginatedAppearanceOptions options => pager.Options;

        /// <summary>
        /// The page count.
        /// </summary>
        private readonly int pages;

        /// <summary>
        /// The current page.
        /// </summary>
        private int page = 1;
        
        /// <summary>
        /// The paginated message
        /// </summary>
        private readonly PaginatedMessage pager;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="PaginatedMessageCallback"/> class.
        /// </summary>
        /// <param name="interactive">
        /// The interactive.
        /// </param>
        /// <param name="sourceContext">
        /// The source context.
        /// </param>
        /// <param name="pager">
        /// The pager.
        /// </param>
        /// <param name="criterion">
        /// The criterion.
        /// </param>
        public PaginatedMessageCallback(InteractiveService interactive, 
            SocketCommandContext sourceContext,
            PaginatedMessage pager,
            ICriterion<SocketReaction> criterion = null)
        {
            Interactive = interactive;
            Context = sourceContext;
            Criterion = criterion ?? new EmptyCriterion<SocketReaction>();
            this.pager = pager;
            pages = this.pager.Pages.Count();
        }

        /// <summary>
        /// Gets the command context.
        /// </summary>
        public SocketCommandContext Context { get; }

        /// <summary>
        /// Gets the interactive service.
        /// </summary>
        public InteractiveService Interactive { get; }
        
        /// <summary>
        /// Gets the criterion.
        /// </summary>
        public ICriterion<SocketReaction> Criterion { get; }
        
        /// <summary>
        /// Gets the message.
        /// </summary>
        public IUserMessage Message { get; private set; }

        /// <summary>
        /// The display async.
        /// </summary>
        /// <param name="reactionList">
        /// The reactions.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task DisplayAsync(ReactionList reactionList)
        {
            var embed = BuildEmbed();
            var message = await Context.Channel.SendMessageAsync(pager.Content, embed: embed).ConfigureAwait(false);
            Message = message;
            Interactive.AddReactionCallback(message, this);

            // reactionList take a while to add, don't wait for them
            _ = Task.Run(async () =>
            {
                if (reactionList.First) await message.AddReactionAsync(options.First);
                if (reactionList.Backward) await message.AddReactionAsync(options.Back);
                if (reactionList.Forward) await message.AddReactionAsync(options.Next);
                if (reactionList.Last) await message.AddReactionAsync(options.Last);


                var manageMessages = Context.Channel is IGuildChannel guildChannel &&
                                     (Context.User as IGuildUser).GetPermissions(guildChannel).ManageMessages;

                if (reactionList.Jump)
                {
                    if (options.JumpDisplayOptions == JumpDisplayOptions.Always || (options.JumpDisplayOptions == JumpDisplayOptions.WithManageMessages && manageMessages))
                    {
                        await message.AddReactionAsync(options.Jump);
                    }
                }

                if (reactionList.Trash)
                {
                    await message.AddReactionAsync(options.Stop);
                }

                if (reactionList.Info)
                {
                    if (options.DisplayInformationIcon) await message.AddReactionAsync(options.Info);
                }
            });
            if (Timeout.HasValue)
            {
                DisplayTimeout(message, Message);
            }
        }

        /// <summary>
        /// Ensures that display is removed on timeout
        /// </summary>
        /// <param name="m1">
        /// The m 1.
        /// </param>
        /// <param name="m2">
        /// The m 2.
        /// </param>
        public void DisplayTimeout(RestUserMessage m1, IUserMessage m2)
        {
            if (Timeout.HasValue)
            {
                _ = Task.Delay(Timeout.Value).ContinueWith(_ =>
                {
                    Interactive.RemoveReactionCallback(m1);
                    m2.DeleteAsync();
                });
            }
        }

        /// <summary>
        /// Handles a reaction callback
        /// </summary>
        /// <param name="reaction">
        /// The reaction.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task<bool> HandleCallbackAsync(SocketReaction reaction)
        {
            var emote = reaction.Emote;

            if (emote.Equals(options.First))
                page = 1;
            else if (emote.Equals(options.Next))
            {
                if (page >= pages)
                    return false;
                ++page;
            }
            else if (emote.Equals(options.Back))
            {
                if (page <= 1)
                    return false;
                --page;
            }
            else if (emote.Equals(options.Last))
                page = pages;
            else if (emote.Equals(options.Stop))
            {
                await Message.DeleteAsync().ConfigureAwait(false);
                return true;
            }
            else if (emote.Equals(options.Jump))
            {
                _ = Task.Run(async () =>
                {
                    var criteria = new Criteria<SocketMessage>()
                        .AddCriterion(new EnsureSourceChannelCriterion())
                        .AddCriterion(new EnsureFromUserCriterion(reaction.UserId))
                        .AddCriterion(new EnsureIsIntegerCriterion());
                    var response = await Interactive.NextMessageAsync(Context, criteria, TimeSpan.FromSeconds(15));
                    var request = int.Parse(response.Content);
                    if (request < 1 || request > pages)
                    {
                        _ = response.DeleteAsync().ConfigureAwait(false);
                        await Interactive.ReplyAndDeleteAsync(Context, options.Stop.Name);
                        return;
                    }

                    page = request;
                    _ = response.DeleteAsync().ConfigureAwait(false);
                    await RenderAsync().ConfigureAwait(false);
                });
            }
            else if (emote.Equals(options.Info))
            {
                await Interactive.ReplyAndDeleteAsync(Context, options.InformationText, timeout: options.InfoTimeout);
                return false;
            }

            _ = Message.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
            await RenderAsync().ConfigureAwait(false);
            return false;
        }

        /// <summary>
        /// The build embed.
        /// </summary>
        /// <returns>
        /// The <see cref="Embed"/>.
        /// </returns>
        protected Embed BuildEmbed()
        {
            var current = pager.Pages.ElementAt(page - 1);

            var builder = new EmbedBuilder
            {
                Author = current.Author ?? pager.Author,
                Title = current.Title ?? pager.Title,
                Url = current.Url ?? pager.Url,
                Description = current.Description ?? pager.Description,
                ImageUrl = current.ImageUrl ?? pager.ImageUrl,
                Color = current.Color ?? pager.Color,
                Fields = current.Fields ?? pager.Fields,
                Footer = current.FooterOverride ?? pager.FooterOverride ?? new EmbedFooterBuilder
                {
                    Text = string.Format(options.FooterFormat, page, pages)
                },
                ThumbnailUrl = current.ThumbnailUrl ?? pager.ThumbnailUrl,
                Timestamp = current.TimeStamp ?? pager.TimeStamp
            };

            /*var builder = new EmbedBuilder()
                .WithAuthor(pager.Author)
                .WithColor(pager.Color)
                .WithDescription(pager.Pages.ElementAt(page - 1).Description)
                .WithImageUrl(current.ImageUrl ?? pager.DefaultImageUrl)
                .WithUrl(current.Url)
                .WithFooter(f => f.Text = string.Format(options.FooterFormat, page, pages))
                .WithTitle(current.Title ?? pager.Title);*/
            builder.Fields = pager.Pages.ElementAt(page - 1).Fields;

            return builder.Build();
        }

        /// <summary>
        /// Renders an embed page
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        private Task RenderAsync()
        {
            var embed = BuildEmbed();
            return Message.ModifyAsync(m => m.Embed = embed);
        }
    }
}
