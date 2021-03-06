﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

using SlipStream.Data;
using SlipStream.Runtime;

namespace SlipStream
{
    public interface IServiceContext : IDisposable, IEquatable<IServiceContext>
    {
        UserSession UserSession { get; }
        IRuleConstraintEvaluator RuleConstraintEvaluator { get; }
        IUserSessionStore UserSessionService { get; }
        IResource GetResource(string resName);
        int GetResourceDependencyWeight(string resName);
        IDataContext DataContext { get; }
        IResourceContainer Resources { get; }
        ILogger BizLogger { get; }
    }
}
