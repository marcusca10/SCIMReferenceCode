﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Microsoft.SCIM
{
    [DataContract]
    public sealed class Core2Error : ErrorBase
    {
        public Core2Error(
            string detail,
            int status,
            string scimType = null // https://datatracker.ietf.org/doc/html/rfc7644#section-3.12
            )
        {
            this.AddSchema(ProtocolSchemaIdentifiers.Version2Error);

            this.Detail = detail;
            this.Status = status;
            this.ScimType = scimType != null ? scimType : null;
        }
    }
}
