using System.Collections.ObjectModel;

namespace MesSpc.Api.SpcEngine.Models;

/// <summary>Pure display metadata. It never loads resources, formats values or changes calculation culture.</summary>
public sealed record UiDisplayMessage
{
    public string MessageKey { get; }
    public IReadOnlyDictionary<string,string> Args { get; }
    public UiDisplayMessage(string messageKey,IReadOnlyDictionary<string,string>? args=null)
    {
        MessageKey=messageKey;
        Args=new ReadOnlyDictionary<string,string>(args is null?new():new Dictionary<string,string>(args,StringComparer.Ordinal));
    }
}
