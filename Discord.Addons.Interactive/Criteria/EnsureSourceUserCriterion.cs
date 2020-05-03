// ReSharper disable StyleCop.SA1600
namespace Discord.Addons.Interactive
{
    using System.Threading.Tasks;

    using Discord.Commands;
    using Discord.WebSocket;

    public class EnsureSourceUserCriterion : ICriterion<SocketMessage>
    {
        /// <summary>
        /// The judge async.
        /// </summary>
        /// <param name="sourceContext">
        /// The source context.
        /// </param>
        /// <param name="parameter">
        /// The parameter.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public Task<bool> JudgeAsync(SocketCommandContext sourceContext, SocketMessage parameter)
        {
            var ok = sourceContext.User.Id == parameter.Author.Id;
            return Task.FromResult(ok);
        }
    }
}
