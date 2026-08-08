namespace Rcv.Web.Api.Services;

internal static class ResultsCacheKeys
{
    public static string ForPoll(Guid pollId) => $"poll-results:{pollId:N}";
}
