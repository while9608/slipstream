﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using ObjectServer.Model;
using ObjectServer.Utility;
using ObjectServer.Backend;

namespace ObjectServer.Core
{

    [Resource]
    public sealed class AuditLogModel : TableModel
    {
        public const string ModelName = "core.audit_log";

        public AuditLogModel()
            : base(ModelName)
        {
            Fields.ManyToOne("user", "core.user").SetLabel("User");
            Fields.Boolean("marked").SetLabel("Marked As Read")
                .Required().SetDefaultProc(ctx => false);
            Fields.Chars("resource").SetLabel("Resource Name").SetSize(64).Required();
            Fields.BigInteger("resource_id").SetLabel("Resource ID").Required();
            Fields.Chars("description").SetLabel("Description")
                .Required().SetSize(256);
        }

    }
}