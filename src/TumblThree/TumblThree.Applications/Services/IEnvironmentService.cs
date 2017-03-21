using System;
using System.Collections.Generic;

namespace TumblThree.Applications.Services
{
    public interface IEnvironmentService
    {
        string AppSettingsPath { get; }
    }
}
