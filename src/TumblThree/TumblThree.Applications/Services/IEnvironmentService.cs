using System;
using System.Collections.Generic;

namespace TumblThree.Applications.Services
{
    public interface IEnvironmentService
    {
        IReadOnlyList<string> QueueList { get; }

        string AppSettingsPath { get; }
    }
}
