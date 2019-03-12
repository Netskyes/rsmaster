using System.Collections.Generic;

namespace RSMaster.Interfaces
{
    internal interface IRuneScapeForm
    {
        string Email { get; set; }
        string RequestId { get; set; }
        string RequestUrl { get; }
        Dictionary<string, string> Build();
    }
}
