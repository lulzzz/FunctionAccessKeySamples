//===============================================================================
// Microsoft FastTrack for Azure
// Function Access Key Samples
//===============================================================================
// Copyright © Microsoft Corporation.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//===============================================================================
using System.Collections.Generic;

namespace AccessKeyCSharp
{
    public class Key
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class Link
    {
        public string rel { get; set; }
        public string href { get; set; }
    }

    public class FunctionKeys
    {
        public List<Key> keys { get; set; }
        public List<Link> links { get; set; }
    }
}
