// ReSharper disable StyleCop.SA1600
namespace Discord.Addons.Interactive
{
    using System;
    using System.Threading.Tasks;

    using Discord.Commands;
    using Discord.WebSocket;

    public interface IReactionCallback
    {
        RunMode RunMode { get; }

        ICriterion<SocketReaction> Criterion { get; }

        TimeSpan? Timeout { get; }

        SocketCommandContext Context { get; }

        Task<bool> HandleCallbackAsync(SocketReaction reaction);
    }
}
